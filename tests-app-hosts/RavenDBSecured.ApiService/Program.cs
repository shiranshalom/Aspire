
using CommunityToolkit.Aspire.RavenDB.Client;

var builder = Host.CreateEmptyApplicationBuilder(null);

builder.Configuration.AddInMemoryCollection([
    new KeyValuePair<string, string?>("ConnectionStrings:ravenSecuredServer", "https://a.shirancontainer.development.run")
]);

var settings = new RavenDBSettings(new[] {"https://a.shirancontainer.development.run"}, "TestSecuredDatabase");
builder.AddRavenDBClient(settings);
builder.Build();