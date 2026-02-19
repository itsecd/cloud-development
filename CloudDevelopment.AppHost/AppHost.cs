using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("credit-order-cache")
    .WithRedisInsight(containerName: "credit-order-insight");

var generator = builder.AddProject<Projects.Generator>("generator")
    .WithReference(cache, "RedisCache")
    .WaitFor(cache);

var client = builder.AddProject<Projects.Client_Wasm>("credit-order-wasm")
    .WithReference(generator)
    .WaitFor(generator);

builder.Build().Run();
