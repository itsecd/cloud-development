var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var client = builder.AddProject<Projects.Client_Wasm>("client");

var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithEnvironment("Cors__AllowedOrigin", client.GetEndpoint("http"));

for (var i = 0; i < 3; i++)
{
    var replica = builder.AddProject<Projects.GeneratorService>($"generator-service-{i}", launchProfileName: null)
        .WithHttpEndpoint(port: 15000 + i)
        .WithReference(redis)
        .WaitFor(redis);
    gateway.WithReference(replica).WaitFor(replica);
}

builder.Build().Run();
