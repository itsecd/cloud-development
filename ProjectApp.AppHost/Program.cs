var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiReplica1 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-r1")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 7001);

var apiReplica2 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-r2")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 7002);

var apiReplica3 = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api-r3")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 7003);

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway")
    .WithReference(apiReplica1)
    .WithReference(apiReplica2)
    .WithReference(apiReplica3)
    .WaitFor(apiReplica1)
    .WaitFor(apiReplica2)
    .WaitFor(apiReplica3)
    .WithEndpoint("http", endpoint => endpoint.Port = 7000);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
