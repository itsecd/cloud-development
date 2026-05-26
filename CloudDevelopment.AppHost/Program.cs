var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var api = builder.AddProject<Projects.ContractGenerator_Api>("api")
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpsEndpoint(port: 7290)
    .WithHttpEndpoint(port: 5290);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(api);

builder.Build().Run();
