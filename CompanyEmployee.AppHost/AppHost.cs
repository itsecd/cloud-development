var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.CompanyEmployee_Gateway>("gateway")
    .WithEndpoint("https", e => e.Port = 7000, createIfNotExists: true)
    .WithExternalHttpEndpoints();

const int startApiPort = 6001;
const int replicaCount = 5;

for (var i = 0; i < replicaCount; i++)
{
    var port = startApiPort + i;
    var url = "https://localhost:" + port.ToString();

    var api = builder.AddProject<Projects.CompanyEmployee_Api>($"api-{i + 1}")
        .WithReference(redis)
        .WithEndpoint("https", e => e.Port = port, createIfNotExists: true)
        .WithEnvironment("ASPNETCORE_URLS", url)
        .WaitFor(redis);

    gateway.WaitFor(api);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WithEnvironment("API_URL", "https://localhost:7000")
    .WaitFor(gateway);

builder.Build().Run();