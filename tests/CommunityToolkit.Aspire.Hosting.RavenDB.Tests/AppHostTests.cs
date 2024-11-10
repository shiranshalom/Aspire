using Aspire.Components.Common.Tests;
using CommunityToolkit.Aspire.Testing;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace CommunityToolkit.Aspire.Hosting.RavenDB.Tests;

[RequiresDocker]
public class AppHostTests(AspireIntegrationTestFixture<Projects.RavenDB_AppHost> fixture) : IClassFixture<AspireIntegrationTestFixture<Projects.RavenDB_AppHost>>
{
    [Fact]
    public async Task TestAppHost()
    {
        using var cancellationToken = new CancellationTokenSource();
        cancellationToken.CancelAfter(TimeSpan.FromMinutes(5));

        var resourceName = "ravenServer";
        await fixture.ResourceNotificationService.WaitForResourceAsync(resourceName, KnownResourceStates.Running, cancellationToken.Token).WaitAsync(TimeSpan.FromMinutes(5), cancellationToken.Token);

        var connectionString = fixture.GetEndpoint(resourceName, "http");
        Assert.NotNull(connectionString);

        var appModel = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var serverResource = Assert.Single(appModel.Resources.OfType<RavenDBServerResource>());
        var dbResource = Assert.Single(appModel.Resources.OfType<RavenDBDatabaseResource>());

        var url = await serverResource.ConnectionStringExpression.GetValueAsync(cancellationToken.Token);
        Assert.NotNull(url);
        Assert.Equal(connectionString.OriginalString, url);

        using var documentStore = new DocumentStore
        {
            Urls = new[] { url }, // Container URL
            Database = dbResource.DatabaseName
        };
        documentStore.Initialize();
        await documentStore.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(dbResource.DatabaseName)), cancellationToken.Token);

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
    }
}
