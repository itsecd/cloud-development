var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var apiService = builder.AddProject<Projects.Employee_ApiService>("apiservice")
    .WithReference(redis)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Client_Wasm>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();