var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithRedisCommander();

var api = builder.AddProject<Projects.Vehicle_Api>("vehicle-api")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(api);

builder.Build().Run();