using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);


// Configuration
var apiServiceConfig = builder.Configuration.GetSection("ApiService");
var ports = apiServiceConfig.GetSection("Ports").Get<List<int>>() ?? [];

var apiGatewayConfig = builder.Configuration.GetSection("ApiGateway");
var gatewayPort = apiGatewayConfig.GetValue<int>("Port");


// Cache (Redis)
var redis = builder.AddRedis("course-cache")
    .WithRedisInsight(containerName: "course-insight")
    .WithDataVolume();


// API services (Backend)
var apiServices = new List<IResourceBuilder<ProjectResource>>();

var serviceId = 1;
foreach(var port in ports)
{
    apiServices.Add(builder.AddProject<Projects.CourseManagement_ApiService>($"course-api-{serviceId++}")
        .WithReference(redis)
        .WithHttpHealthCheck("/health")
        .WithHttpEndpoint(port: port, name: "course-api-endpoint", isProxied: false)
        .WithExternalHttpEndpoints());
}


// API Gateway (Ocelot)
var apiGateway = builder.AddProject<Projects.CourseManagement_ApiGateway>("course-gateway")
    .WithHttpEndpoint(port: gatewayPort, name: "course-gateway-endpoint", isProxied: false)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

foreach (var apiService in apiServices)
{
    apiGateway.WaitFor(apiService);
}


// Client (Frontend)
builder.AddProject<Projects.Client_Wasm>("course-wasm")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiGateway)
    .WaitFor(apiGateway);


builder.Build().Run();
