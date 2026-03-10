var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander(containerName: "redis-commander");

var gateway = builder.AddProject<Projects.CompanyEmployee_Gateway>("gateway")
    .WithEndpoint("https", e => e.Port = 7001)
    .WithExternalHttpEndpoints();

const int startApiPort = 6001;
const int replicaCount = 5;

for (var i = 0; i < replicaCount; i++)
{
    var port = startApiPort + i;
    var api = builder.AddProject<Projects.CompanyEmployee_Api>($"api-{i + 1}")
        .WithEndpoint("https", e => e.Port = port)
        .WithReference(redis)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WaitFor(redis);

    gateway.WaitFor(api);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WithEnvironment("API_URL", "https://localhost:7001")
    .WaitFor(gateway);

builder.Build().Run();