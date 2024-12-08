using Aspire.Components.Common.Tests;
using CommunityToolkit.Aspire.RavenDB.Client;
using CommunityToolkit.Aspire.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using System.Security.Cryptography.X509Certificates;

namespace CommunityToolkit.Aspire.Hosting.RavenDB.Tests;

[RequiresDocker]
public class SecuredAppHostTests(AspireIntegrationTestFixture<Projects.RavenDBSecured_AppHost> fixture) : IClassFixture<AspireIntegrationTestFixture<Projects.RavenDBSecured_AppHost>>
{
    [Fact]
    public async Task TestSecuredAppHost()
    {
        using var cancellationToken = new CancellationTokenSource();
        cancellationToken.CancelAfter(TimeSpan.FromMinutes(5));

        var resourceName = "ravenSecuredServer";
        var databaseName = "TestSecuredDatabase";

        await fixture.ResourceNotificationService.WaitForResourceAsync(resourceName, KnownResourceStates.Running, cancellationToken.Token).WaitAsync(TimeSpan.FromMinutes(5), cancellationToken.Token);

        var connectionString = fixture.GetEndpoint(resourceName, "https");
        Assert.NotNull(connectionString);

        var appModel = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var serverResource = Assert.Single(appModel.Resources.OfType<RavenDBServerResource>());
        var dbResource = Assert.Single(appModel.Resources.OfType<RavenDBDatabaseResource>());

        var url = await serverResource.ConnectionStringExpression.GetValueAsync(cancellationToken.Token);
        Assert.NotNull(url);
        Assert.Equal(connectionString.OriginalString, url);
        Assert.Equal(databaseName, dbResource.DatabaseName);


        await Task.Delay(10000, cancellationToken.Token);

        // Connect to RavenDB Client

        var clientBuilder = Host.CreateEmptyApplicationBuilder(null);
        clientBuilder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>($"ConnectionStrings:{serverResource.Name}", "https://a.shirancontainer.development.run")
        ]);

#pragma warning disable SYSLIB0057
        using var certificate = new X509Certificate2("C:/RavenDB/Server/Security/cluster.server.certificate.shirancontainer.pfx");
#pragma warning restore SYSLIB0057
        var settings = new RavenDBSettings(urls: new[] { "https://a.shirancontainer.development.run" }, databaseName: databaseName)
        {
            CreateDatabase = true,
            Certificate = certificate
        };

        clientBuilder.AddRavenDBClient(settings);
        var host = clientBuilder.Build();

        using var documentStore = host.Services.GetRequiredService<IDocumentStore>();

        using (var session = documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(new { Id = "Test/1", Name = "Test Document" }, cancellationToken.Token);
            await session.SaveChangesAsync(cancellationToken.Token);
        }

        using (var session = documentStore.OpenAsyncSession())
        {
            var doc = await session.LoadAsync<dynamic>("Test/1", cancellationToken.Token);
            Assert.NotNull(doc);
            Assert.Equal("Test Document", doc.Name.ToString());
        }

        documentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(databaseName, true));
    }
}
