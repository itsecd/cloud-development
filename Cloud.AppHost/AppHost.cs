var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var api = builder.AddProject<Projects.Cloud_API>("api")
    .WithReference(redis)
    .WaitFor(redis);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(api);

builder.Build().Run();
