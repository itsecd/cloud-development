var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("cache").WithRedisInsight();

var back = builder.AddProject<Projects.Server>("back")
    .WithReference(redis)
    .WaitFor(redis);

var gateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithReference(back)
    .WaitFor(back);

builder.AddProject<Projects.Client_Wasm>("front")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();