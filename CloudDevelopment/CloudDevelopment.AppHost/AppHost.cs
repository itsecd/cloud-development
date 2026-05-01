var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var generation = builder.AddProject<Projects.GenerationService>("generation-service")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(generation)
    .WaitFor(generation);

builder.Build().Run();