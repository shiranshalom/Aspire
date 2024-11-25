var builder = DistributedApplication.CreateBuilder(args);

builder.AddRavenDB("ravenServer").AddDatabase("TestDatabase");

builder.Build().Run();