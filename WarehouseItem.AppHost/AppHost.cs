var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("warehouse-item-cache")
    .WithRedisInsight(containerName: "warehouse-item-insight");

var gateway = builder.AddProject<Projects.WarehouseItem_Gateway>("gateway")
    .WithHttpEndpoint(name: "http", port: 5300)
    .WithExternalHttpEndpoints();

const int generatorPortBase = 5200;
for (var i = 1; i <= 5; ++i)
{
    var generator = builder.AddProject<Projects.WarehouseItem_Generator>($"generator-{i}")
        .WithReference(cache, "warehouse-item-cache")
        .WithHttpEndpoint(name: "http", port: generatorPortBase + i)
        .WaitFor(cache);

    gateway.WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();