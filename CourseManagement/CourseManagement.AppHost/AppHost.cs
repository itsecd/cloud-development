var builder = DistributedApplication.CreateBuilder(args);

// Redis
var redis = builder.AddRedis("course-cache")
    .WithRedisCommander(containerName: "redis-commander")
    .WithDataVolume();

// API service
var apiService = builder.AddProject<Projects.CourseManagement_ApiService>("course-insight")
    .WithReference(redis)
    .WithHttpHealthCheck("/health");

// Client
builder.AddProject<Projects.Client_Wasm>("course-wasm")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
