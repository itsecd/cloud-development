var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("warehouse-item-cache")
    .WithRedisInsight(containerName: "warehouse-item-insight");

var generator = builder.AddProject<Projects.WarehouseItem_Generator>("generator")
    .WithReference(cache, "warehouse-item-cache")
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(generator)
    .WaitFor(generator);

builder.Build().Run();
