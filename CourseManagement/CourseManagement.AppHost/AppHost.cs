var builder = DistributedApplication.CreateBuilder(args);

// Redis
var redis = builder.AddRedis("course-cache");

// API service
var apiService = builder.AddProject<Projects.CourseManagement_ApiService>("course-service")
    .WithReference(redis)
    .WithHttpHealthCheck("/health");

// Client
builder.AddProject<Projects.Client_Wasm>("course-management")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
