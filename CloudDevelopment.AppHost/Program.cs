var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("course-cache")
    .WithRedisInsight(containerName: "course-insight");
var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for(var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"service-api-{i}", launchProfileName:null)
    .WithReference(cache, "RedisCache")
    .WaitFor(cache)
    .WithHttpsEndpoint(port:5666+i);
    gateway.WaitFor(service).WithReference(service);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
