var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var api = builder.AddProject<Projects.WarehouseApp_Api>("warehouseapp-api")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(api);

builder.Build().Run();
