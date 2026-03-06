var builder = DistributedApplication.CreateBuilder(args);

// Cache (Redis)
var redis = builder.AddRedis("course-cache")
    .WithRedisInsight(containerName: "course-insight")
    .WithDataVolume();

// API services (Backend)
var apiService1 = builder.AddProject<Projects.CourseManagement_ApiService>("course-api-1")
    .WithReference(redis)
    .WithHttpHealthCheck("/health")
    .WithHttpEndpoint(port: 8081, name: "course-api-endpoint", isProxied: false)
    .WithExternalHttpEndpoints();

var apiService2 = builder.AddProject<Projects.CourseManagement_ApiService>("course-api-2")
    .WithReference(redis)
    .WithHttpHealthCheck("/health")
    .WithHttpEndpoint(port: 8082, name: "course-api-endpoint", isProxied: false)
    .WithExternalHttpEndpoints();

var apiService3 = builder.AddProject<Projects.CourseManagement_ApiService>("course-api-3")
    .WithReference(redis)
    .WithHttpHealthCheck("/health")
    .WithHttpEndpoint(port: 8083, name: "course-api-endpoint", isProxied: false)
    .WithExternalHttpEndpoints();

// API Gateway (Ocelot)
var apiGateway = builder.AddProject<Projects.CourseManagement_ApiGateway>("course-gateway")
    .WithReference(apiService1)
    .WithReference(apiService2)
    .WithReference(apiService3)
    .WithHttpEndpoint(port: 5000, name: "course-gateway-endpoint", isProxied: false)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

// Client (Frontend)
builder.AddProject<Projects.Client_Wasm>("course-wasm")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();
