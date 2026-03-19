var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("cache").WithRedisInsight();

var back = builder.AddProject<Projects.Server>("back")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("front")
    .WaitFor(back);
builder.Build().Run();
