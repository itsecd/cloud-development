var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var generationApi = builder.AddProject<Projects.ProjectGenerator_Api>("generation-api")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(generationApi);

builder.Build().Run();
