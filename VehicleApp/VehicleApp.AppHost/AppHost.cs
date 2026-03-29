var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var api = builder.AddProject<Projects.VehicleApp_Api>("vehicleapp-api")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(api);

builder.Build().Run();
