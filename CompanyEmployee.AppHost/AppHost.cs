var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "redis-insight");

var generator = builder.AddProject<Projects.CompanyEmployee_Generator>("generator")
    .WithReference(cache)
    .WaitFor(cache);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(generator)
    .WaitFor(generator);

builder.Build().Run();