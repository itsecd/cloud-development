var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.Generator>("generator").WithReference(redis);

builder.Build().Run();
