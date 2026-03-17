var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var client = builder.AddProject<Projects.Client_Wasm>("client");

builder.AddProject<Projects.GeneratorService>("generator-service")
    .WithReference(redis)
    .WaitFor(redis)
    .WithEnvironment("Cors__AllowedOrigin", client.GetEndpoint("http"))
    .WaitFor(client);

builder.Build().Run();