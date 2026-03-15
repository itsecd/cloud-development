var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var api = builder.AddProject<Projects.VehicleApi>("vehicleapi")
    .WithReference(cache);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api);

builder.Build().Run();
