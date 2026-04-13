var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var api = builder.AddProject<Projects.ProjectApp_Api>("projectapp-api")
    .WithReference(redis)
    .WithReplicas(5)
    .WaitFor(redis);

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WithEnvironment("BaseAddress", gateway.GetEndpoint("http"))
    .WaitFor(gateway);

builder.Build().Run();
