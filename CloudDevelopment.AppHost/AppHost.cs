using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var generator = builder.AddProject<Projects.Generator>("generator").WithReference(redis);

var client = builder.AddProject<Projects.Client_Wasm>("client").WithReference(generator);

builder.Build().Run();
