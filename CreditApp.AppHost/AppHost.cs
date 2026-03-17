var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander(containerName: "redis-commander");

var gateway = builder.AddProject<Projects.CreditApp_Gateway>("gateway")
    .WithEndpoint("https", e =>
    {
        e.Port = 9001;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithExternalHttpEndpoints();

const int startApiPort = 7401;
const int replicaCount = 5;

for (var i = 0; i < replicaCount; i++)
{
    var port = startApiPort + i;
    var api = builder.AddProject<Projects.CreditApp_Api>($"api{i + 1}")
        .WithEndpoint("https", e =>
        {
            e.Port = port;
            e.IsProxied = false;
            e.UriScheme = "https";
        })
        .WithReference(redis)
        .WaitFor(redis);

    gateway.WaitFor(api);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();