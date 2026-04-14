var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway");

var replicaWeights = new[] { 1, 2, 3, 2, 1 };

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 15000 + i)
        .WithReference(cache, "RedisCache")
        .WithEnvironment("ReplicaId", "R" + (i + 1))
        .WithEnvironment("ReplicaWeight", replicaWeights[i].ToString())
        .WaitFor(cache);

    gateway.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("employee")
    .WaitFor(gateway);

builder.Build().Run();