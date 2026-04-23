var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"employee-api-{i + 1}", launchProfileName: null)
        .WithHttpsEndpoint(7170 + i)
        .WithReference(cache, "RedisCache")
        .WaitFor(cache);
    gateway.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("employee-wasm")
    .WaitFor(gateway);



builder.Build().Run();
