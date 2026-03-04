var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithRedisInsight(containerName: "cache-insight");

var generationService = builder.AddProject<Projects.GenerationService>("generation-service")
    .WithReference(cache, "RedisCache")
    .WaitFor(cache)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(generationService);

builder.Build().Run();
