var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("programproj-cache").WithRedisInsight(containerName: "programproj-insight");

var apiService = builder.AddProject<Projects.Service_Api>("programproj-api")
    .WithReference(cache)
    .WithHttpHealthCheck("/health")
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("programproj-wasm")
    .WaitFor(apiService);

builder.Build().Run();
