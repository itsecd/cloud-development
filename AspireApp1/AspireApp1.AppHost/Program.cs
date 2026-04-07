var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("project-cache")
    .WithRedisInsight(containerName: "project-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5;  i++)
{
    var service = builder.AddProject<Projects.ServiceApi>($"programproject-api-{i + 1}", launchProfileName: null)
        .WithHttpsEndpoint(4440 + i)
        .WithReference(cache, "RedisCache")
        .WaitFor(cache);
    gateway.WaitFor(service);   
}

builder.AddProject<Projects.Client_Wasm>("programproject-wasm")
    .WaitFor(gateway);

builder.Build().Run();