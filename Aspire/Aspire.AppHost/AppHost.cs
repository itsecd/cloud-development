using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("programproj-cache").WithRedisInsight(containerName: "programproj-insight");

var ports = builder.Configuration.GetSection("ApiGateway:Ports").Get<int[]>() ?? throw new InvalidOperationException("api gw ports r not configured");
var apiServices = new List<IResourceBuilder<ProjectResource>>();
for (var i = 0; i < ports.Length; i++)
{
    var httspPort = ports[i];
    var httpPort = httspPort - 1000;
    apiServices.Add(builder.AddProject<Projects.Service_Api>($"programproj-api{i+1}", project => { project.ExcludeLaunchProfile = true; })
        .WithReference(cache, "RedisCache")
        .WithHttpEndpoint(port: httpPort)
        .WithHttpsEndpoint(port: httspPort)
        .WithHttpHealthCheck("/health", endpointName: "https")
        .WaitFor(cache));
}
var apiGW = builder.AddProject<Projects.Service_ApiGw>("api-gw", project => { project.ExcludeLaunchProfile = true; })
    .WithHttpEndpoint(port: 5247)
    .WithHttpsEndpoint(port: 7198);
foreach (var service in apiServices) apiGW.WaitFor(service);
builder.AddProject<Projects.Client_Wasm>("programproj-wasm")
    .WithReference(apiGW)
    .WaitFor(apiGW);

builder.Build().Run();
