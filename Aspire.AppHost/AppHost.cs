var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("cache").WithRedisInsight();

var gateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithEndpoint("https", endpoint => endpoint.Port = 8095);

for (var i = 0; i < 3; i++)
{
    var generator = builder.AddProject<Projects.Server>($"back-{i}")
    .WithEndpoint("https", endpoint => endpoint.Port = 8090+i)
    .WithReference(redis)
    .WaitFor(redis);

    gateway.WithReference(generator)
        .WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("front")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();