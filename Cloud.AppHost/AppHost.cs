var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var gateway = builder.AddProject<Projects.API_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.Cloud_API>($"api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 8000 + i)
        .WithReference(redis)
        .WaitFor(redis);
    gateway.WaitFor(api);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
