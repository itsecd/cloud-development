var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisCommander(); 

var api = builder.AddProject<Projects.Inventory_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache);

var client = builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(api);

builder.Build().Run();