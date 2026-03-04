var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var api = builder.AddProject<Projects.VehicleApp_Api>("vehicleapp-api")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
