using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using CommunityToolkit.Aspire.Hosting.RavenDB;
using HealthChecks.RavenDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding RavenDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class RavenDBBuilderExtensions
{
    /// <summary>
    /// Adds a RavenDB server resource to the application model. A container is used for local development.
    /// This version of the package defaults to the <inheritdoc cref="RavenDBContainerImageTags.Tag"/> tag of the <inheritdoc cref="RavenDBContainerImageTags.Image"/> container image.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/></param>
    /// <param name="name">The name of the RavenDB server resource.</param>
    /// <param name="secured">Indicates whether the server connection should be secured (HTTPS). Defaults to false.</param>
    /// <param name="environmentVariables">Optional environment variables to configure the RavenDB server.</param>
    /// <param name="port">Optional port for the server. If not provided, defaults to the container's internal port (8080).</param>
    /// <returns>A resource builder for the newly added RavenDB server resource.</returns>
    /// <exception cref="DistributedApplicationException">Thrown when the connection string cannot be retrieved during configuration.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection string is unavailable.</exception>
    public static IResourceBuilder<RavenDBServerResource> AddRavenDB(this IDistributedApplicationBuilder builder,
    [ResourceName] string name,
    bool secured = false,
    Dictionary<string, object>? environmentVariables = null,
    int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var serverResource = new RavenDBServerResource(name, secured);

        string? connectionString = null;
        builder.Eventing.Subscribe<ConnectionStringAvailableEvent>(serverResource, async (@event, ct) =>
        {
            connectionString = await serverResource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{serverResource.Name}' resource but the connection string was null.");
        });

        var healthCheckKey = $"{name}_check";

        // TODO: Use AddRavenDB (Microsoft.Extensions.DependencyInjection).
        // We should create an overload with 'Func<IServiceProvider, IHealthCheck> factory' parameter 
        // for using the connectionString once it is available 
        
        //builder.Services.AddHealthChecks().AddRavenDB(sp => sp.Urls = new []{ connectionString ?? throw new InvalidOperationException("Connection string is unavailable") },
        //name: healthCheckKey);

        builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            healthCheckKey,
            _ => new RavenDBHealthCheck(new RavenDBOptions { Urls = new[] { connectionString ?? throw new InvalidOperationException("Connection string is unavailable") } }),
            failureStatus: default,
            tags: default,
            timeout: default));

        return builder
            .AddResource(serverResource)
            .WithEndpoint(port: port, targetPort: 8080, scheme: serverResource.PrimaryEndpoint.EndpointName)
            .WithImage(RavenDBContainerImageTags.Image)
            .WithImageRegistry(RavenDBContainerImageTags.Registry)
            .WithEnvironment(context => ConfigureEnvironmentVariables(context, environmentVariables))
            .WithHealthCheck(healthCheckKey);
    }

    private static void ConfigureEnvironmentVariables(EnvironmentCallbackContext context, Dictionary<string, object>? environmentVariables = null)
    {
        if (environmentVariables is null)
        {
            context.EnvironmentVariables.TryAdd("RAVEN_Setup_Mode", "None");
            context.EnvironmentVariables.TryAdd("RAVEN_Security_UnsecuredAccessAllowed", "PrivateNetwork");
            return;
        }

        foreach (var environmentVariable in environmentVariables)
            context.EnvironmentVariables.TryAdd(environmentVariable.Key, environmentVariable.Value);
    }

    /// <summary>
    /// Adds a database resource to an existing RavenDB server resource.
    /// </summary>
    /// <param name="builder">The resource builder for the RavenDB server.</param>
    /// <param name="name">The name of the database resource.</param>
    /// <param name="databaseName">The name of the database to create/add. Defaults to the same name as the resource if not provided.</param>
    /// <returns>A resource builder for the newly added RavenDB database resource.</returns>
    /// <exception cref="DistributedApplicationException">Thrown when the connection string cannot be retrieved during configuration.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the connection string is unavailable.</exception>
    public static IResourceBuilder<RavenDBDatabaseResource> AddDatabase(this IResourceBuilder<RavenDBServerResource> builder,
        [ResourceName] string name,
        string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.AddDatabase(name, databaseName);
        var databaseResource = new RavenDBDatabaseResource(name, databaseName, builder.Resource);

        string? connectionString = null;

        builder.ApplicationBuilder.Eventing.Subscribe<ConnectionStringAvailableEvent>(databaseResource, async (@event, ct) =>
        {
            connectionString = await databaseResource.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

            if (connectionString == null)
                throw new DistributedApplicationException($"ConnectionStringAvailableEvent was published for the '{databaseResource.Name}' resource but the connection string was null.");
        });

        var healthCheckKey = $"{name}_check";

        // TODO: Use AddRavenDB (Microsoft.Extensions.DependencyInjection).
        // We should create an overload with 'Func<IServiceProvider, IHealthCheck> factory' parameter 
        // for using the connectionString once it is available 

        //builder.ApplicationBuilder.Services.AddHealthChecks().AddRavenDB(options =>
        //{
        //    options.Urls = new[] { connectionString ?? throw new InvalidOperationException("Connection string is unavailable") };
        //    options.Database = databaseName;
        //}, name: healthCheckKey);

        builder.ApplicationBuilder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            healthCheckKey,
            _ => new RavenDBHealthCheck(new RavenDBOptions
            {
                Urls = new[] { connectionString ?? throw new InvalidOperationException("Connection string is unavailable") },
                Database = databaseName
            }),
            failureStatus: default,
            tags: default,
            timeout: default));

        return builder.ApplicationBuilder
            .AddResource(databaseResource);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a RavenDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder for the RavenDB server.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">Indicates whether the bind mount should be read-only. Defaults to false.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for the RavenDB server resource.</returns>
    public static IResourceBuilder<RavenDBServerResource> WithDataBindMount(this IResourceBuilder<RavenDBServerResource> builder, string source, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/var/lib/ravendb/data", isReadOnly);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a RavenDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder for the RavenDB server.</param>
    /// <param name="name">Optional name for the volume. Defaults to a generated name if not provided.</param>
    /// <param name="isReadOnly">Indicates whether the volume should be read-only. Defaults to false.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for the RavenDB server resource.</returns>
    public static IResourceBuilder<RavenDBServerResource> WithDataVolume(this IResourceBuilder<RavenDBServerResource> builder, string? name = null, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

#pragma warning disable CTASPIRE001
        return builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/var/lib/ravendb/data", isReadOnly);
#pragma warning restore CTASPIRE001
    }
}
