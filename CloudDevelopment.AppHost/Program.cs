var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.ContractGenerator_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 25000 + i)
        .WithReference(redis)
        .WaitFor(redis);
    gateway.WaitFor(service);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);


builder.Build().Run();
