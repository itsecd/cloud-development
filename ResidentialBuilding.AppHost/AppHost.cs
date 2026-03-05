var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("residential-building-cache")
    .WithRedisInsight(containerName: "residential-building-insight");

var gateway = builder.AddProject<Projects.ResidentialBuilding_Gateway>("gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 5300)
    .WithExternalHttpEndpoints();

const int generatorPortBase = 5200;
for (var i = 1; i <= 5; ++i)
{
    var i1 = i;
    var generator = builder.AddProject<Projects.ResidentialBuilding_Generator>($"generator-{i}")
        .WithReference(cache, "residential-building-cache")
        .WithEndpoint("http", endpoint => endpoint.Port = generatorPortBase + i1)
        .WaitFor(cache);
    
    gateway.WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();