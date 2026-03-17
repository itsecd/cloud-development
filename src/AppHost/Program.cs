var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var api = builder.AddProject<Projects.VehicleApi>("vehicleapi")
    .WithReference(cache)
    .WaitFor(cache);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
