var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "redis-insight");

var gateway = builder.AddProject<Projects.CompanyEmployee_ApiGateway>("apiGateway")
    .WithEndpoint("https", e => e.Port = 7200)
    .WithExternalHttpEndpoints();

const int startGeneratorPort = 7301;
for (var i = 0; i < 5; ++i)
{
    var generator = builder.AddProject<Projects.CompanyEmployee_Generator>($"generator-{i}")
        .WithEndpoint("https", e => e.Port = startGeneratorPort + i)
        .WithReference(cache)
        .WaitFor(cache);

    gateway.WaitFor(generator);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();