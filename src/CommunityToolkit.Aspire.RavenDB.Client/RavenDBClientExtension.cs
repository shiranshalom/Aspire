using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System.Security.Cryptography.X509Certificates;

namespace CommunityToolkit.Aspire.RavenDB.Client;

/// <summary>
/// Extension methods for connecting RavenDB database.
/// </summary>
public static class RavenDBClientExtension
{
    private const string ActivityNameSource = "RavenDB.Client.DiagnosticSources";

    /// <summary>
    /// Registers <see cref="IDocumentStore"/> and the associated <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/>
    /// instances for connecting to an existing or new RavenDB database with RavenDB.Client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> used to add services.</param>
    /// <param name="settings">The settings required to configure the <see cref="IDocumentStore"/>.</param>
    /// <remarks>Notes:
    /// <list type="bullet">
    /// <item><description>If <see cref="RavenDBSettings.DatabaseName"/> is not specified and <see cref="RavenDBSettings.CreateDatabase"/> is set to 'false',
    /// <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/> will not be registered.</description></item>
    /// <item><description>The <see cref="IDocumentStore"/> is registered as a singleton, meaning a single instance is shared throughout the application's lifetime,
    /// while <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/> are registered per request to ensure short-lived session instances for each use.</description></item>
    /// </list>
    /// </remarks>
    public static void AddRavenDBClient(
        this IHostApplicationBuilder builder,
        RavenDBSettings settings)
    {
        ValidateSettings(builder, settings);

        builder.AddRavenDBClient(settings, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="IDocumentStore"/> and the associated <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/>
    /// instances for connecting to an existing or new RavenDB database with RavenDB.Client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> used to add services.</param>
    /// <param name="connectionUrls">The URLs of the RavenDB cluster nodes to connect to.</param>
    /// <param name="databaseName">Optional: the name of an existing database to connect to.
    /// If not specified, <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/> will not be registered,
    /// as a database context is required for session creation.</param>
    /// <param name="certificate">Optional. The certificate for the server.</param>
    public static void AddRavenDBClient(
        this IHostApplicationBuilder builder,
        string[] connectionUrls,
        string? databaseName,
        X509Certificate2? certificate = null)
    {
        var settings = new RavenDBSettings(connectionUrls, databaseName);
        if (certificate != null)
            settings.Certificate = certificate;

        ValidateSettings(builder, settings);

        builder.AddRavenDBClient(settings, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="IDocumentStore"/> and the associated <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/>
    /// instances for connecting to an existing or new RavenDB database with RavenDB.Client, identified by a unique service key.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> used to add services.</param>
    /// <param name="serviceKey">A unique key that identifies this instance of the RavenDB client service.</param>
    /// <param name="settings">The settings required to configure the <see cref="IDocumentStore"/>.</param>
    /// <remarks>Note: If <see cref="RavenDBSettings.DatabaseName"/> is not specified and <see cref="RavenDBSettings.CreateDatabase"/>
    /// is set to 'false', <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/> will not be registered.</remarks>
    public static void AddKeyedRavenDBClient(
        this IHostApplicationBuilder builder,
        object serviceKey,
        RavenDBSettings settings)
    {
        ValidateSettings(builder, settings);

        builder.AddRavenDBClient(settings, serviceKey);
    }

    /// <summary>
    /// Registers <see cref="IDocumentStore"/> and the associated <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/>
    /// instances for connecting to an existing or new RavenDB database with RavenDB.Client, identified by a unique service key.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> used to add services.</param>
    /// <param name="serviceKey">A unique key that identifies this instance of the RavenDB client service.</param>
    /// <param name="connectionUrls">The URLs of the RavenDB cluster nodes to connect to.</param>
    /// <param name="databaseName">Optional: the name of an existing database to connect to.
    /// If not specified, <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/> will not be registered,
    /// as a database context is required for session creation.</param>
    /// <param name="certificate">Optional. The certificate for the server.</param>
    /// <remarks>Note: If <see cref="RavenDBSettings.DatabaseName"/> is not specified and <see cref="RavenDBSettings.CreateDatabase"/>
    /// is set to 'false', <see cref="IDocumentSession"/> and <see cref="IAsyncDocumentSession"/> will not be registered.</remarks>
    public static void AddKeyedRavenDBClient(
        this IHostApplicationBuilder builder,
        object serviceKey,
        string[] connectionUrls,
        string? databaseName,
        X509Certificate2? certificate = null)
    {
        var settings = new RavenDBSettings(connectionUrls, databaseName);
        if (certificate != null)
            settings.Certificate = certificate;

        ValidateSettings(builder, settings);

        builder.AddRavenDBClient(settings, serviceKey);
    }

    private static void AddRavenDBClient(
        this IHostApplicationBuilder builder,
        RavenDBSettings settings,
        object? serviceKey)
    {
        var documentStore = CreateRavenClient(settings);

        if (serviceKey is null)
        {
            builder
                .Services
                .AddSingleton<IDocumentStore>(documentStore);
        }
        else
        {
            builder
                .Services
                .AddKeyedSingleton<IDocumentStore>(serviceKey, documentStore);
        }

        builder.AddRavenDocumentSession(documentStore, serviceKey);

        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing.AddSource(ActivityNameSource);
                });
        }

        builder.AddHealthCheck(
            serviceKey is null ? "RavenDB.Client" : $"RavenDB.Client_{serviceKey}",
            settings);
    }

    private static void AddRavenDocumentSession(
        this IHostApplicationBuilder builder,
        IDocumentStore documentStore,
        object? serviceKey)
    {
        if (string.IsNullOrWhiteSpace(documentStore.Database))
            return;

        // AddTransient creates new instance per request/usage which is ideal for document sessions

        if (serviceKey is null)
        {
            builder.Services.AddTransient<IDocumentSession>(provider =>
                provider.CreateDocumentSession(documentStore));

            builder.Services.AddTransient<IAsyncDocumentSession>(provider =>
                provider.CreateAsyncDocumentSession(documentStore));

            return;
        }

        builder.Services.AddKeyedTransient<IDocumentSession>(serviceKey,
            (sp, _) => sp.CreateDocumentSession(documentStore));

        builder.Services.AddKeyedTransient<IAsyncDocumentSession>(serviceKey,
            (sp, _) => sp.CreateAsyncDocumentSession(documentStore));
    }

    private static void AddHealthCheck(
        this IHostApplicationBuilder builder,
        string healthCheckName,
        RavenDBSettings settings)
    {
        if (settings.DisableHealthChecks)
            return;

        builder.TryAddHealthCheck(
            healthCheckName,
            healthCheck => healthCheck.AddRavenDB(options =>
                {
                    options.Database = settings.DatabaseName;
                    options.Urls = settings.Urls;
                    options.Certificate = settings.GetCertificate();
                },
                healthCheckName,
                null,
                null,
                settings.HealthCheckTimeout > 0 ? TimeSpan.FromMilliseconds(settings.HealthCheckTimeout.Value) : null));
    }

    private static IDocumentStore CreateRavenClient(RavenDBSettings ravenDbSettings)
    {
        var documentStore = new DocumentStore()
        {
            Urls = ravenDbSettings.Urls,
            Database = ravenDbSettings.DatabaseName,
            Certificate = ravenDbSettings.GetCertificate()
        };

        ravenDbSettings.ModifyDocumentStore?.Invoke(documentStore);

        documentStore.Initialize();

        if (ravenDbSettings.CreateDatabase)
        {
            var databaseRecord = new DatabaseRecord(ravenDbSettings.DatabaseName);
            documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(databaseRecord));
        }

        return documentStore;
    }

    private static IDocumentSession CreateDocumentSession(this IServiceProvider provider,
        IDocumentStore documentStore) => documentStore.OpenSession();

    private static IAsyncDocumentSession CreateAsyncDocumentSession(this IServiceProvider provider,
        IDocumentStore documentStore) => documentStore.OpenAsyncSession();

    private static void ValidateSettings(
        IHostApplicationBuilder builder,
        RavenDBSettings settings)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (settings.Urls == null || settings.Urls.Length == 0)
            throw new ArgumentNullException(nameof(settings.Urls), "At least one connection URL must be provided.");

        if (settings.CreateDatabase && string.IsNullOrWhiteSpace(settings.DatabaseName))
            throw new ArgumentNullException(nameof(settings.DatabaseName), "A database name must be specified in 'RavenDBSettings.DatabaseName' " +
                                                                           "when 'RavenDBSettings.CreateDatabase' is set to true.");

        foreach (var url in settings.Urls)
        {
            if (IsValidUrl(url, out var uri) == false)
                throw new ArgumentException($"The provided URL '{url}' is invalid. Please provide a valid HTTP or HTTPS URL.");

            if (uri!.Scheme == Uri.UriSchemeHttps)
            {
                if (string.IsNullOrEmpty(settings.CertificatePath) && settings.Certificate == null)
                    throw new ArgumentNullException(nameof(settings.Certificate), "A valid certificate must be provided in 'RavenDBSettings.Certificate' " +
                        "or a certificate path in 'RavenDBSettings.CertificatePath' when using HTTPS.");
            }
        }

        bool IsValidUrl(string url, out Uri? uriResult)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
