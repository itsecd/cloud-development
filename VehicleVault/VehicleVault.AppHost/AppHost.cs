var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var api = builder.AddProject<Projects.VehicleVault_Api>("vehiclevault-api")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(api);

builder.Build().Run();
