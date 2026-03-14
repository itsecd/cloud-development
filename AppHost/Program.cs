var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.GeneratorService>("generator-service")
    .WithReference(redis);

builder.AddProject<Projects.Client_Wasm>("client");

builder.Build().Run();
