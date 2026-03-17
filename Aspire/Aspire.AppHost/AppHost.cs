using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("programproj-cache").WithRedisInsight(containerName: "programproj-insight");

var ports = builder.Configuration.GetSection("ApiGateway:Ports").Get<int[]>() ?? throw new InvalidOperationException("Configuration section 'ApiGateway:Ports' is missing or empty in appsettings.json file.");
var apiGW = builder.AddProject<Projects.Service_Gateway>("api-gw", project => { project.ExcludeLaunchProfile = true; })
    .WithHttpEndpoint(port: 5247)
    .WithHttpsEndpoint(port: 7198);
for (var i = 0; i < ports.Length; i++)
{
    var httspPort = ports[i];
    var httpPort = httspPort - 1000;
    var service = builder.AddProject<Projects.Service_Api>($"programproj-api{i + 1}", project => { project.ExcludeLaunchProfile = true; })
        .WithReference(cache, "RedisCache")
        .WithHttpEndpoint(port: httpPort)
        .WithHttpsEndpoint(port: httspPort)
        .WithHttpHealthCheck("/health", endpointName: "https")
        .WaitFor(cache);
    apiGW.WaitFor(service);
}
builder.AddProject<Projects.Client_Wasm>("programproj-wasm")
    .WaitFor(apiGW);

builder.AddProject<Projects.Service_Gateway>("service-gateway");

builder.Build().Run();
