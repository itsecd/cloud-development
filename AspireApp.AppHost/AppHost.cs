var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("RedisCache").WithRedisInsight(containerName: "insight");

var ports = new[] { 5001, 5002, 5003 };

var gateway = builder.AddProject<Projects.AspireApp_ApiGateway>("api-gateway")
    .WithHttpEndpoint(port: 5101, name: "gateway");

for (var i = 0; i < 3; i++)
{
    var api = builder.AddProject<Projects.AspireApp_ApiService>($"warehouse-api-{i}")
        .WithReference(cache)
        .WithEnvironment("REPLICA_ID", i.ToString())
        .WithHttpEndpoint(port: ports[i], name: $"api-{i}")
        .WaitFor(cache);

    gateway = gateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(gateway)
    .WithHttpEndpoint(port: 5127, name: "client")
    .WaitFor(gateway);

builder.Build().Run();