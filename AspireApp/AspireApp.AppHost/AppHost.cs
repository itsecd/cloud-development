var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

var service = builder.AddProject<Projects.Service_Api>("service-api")
    .WithReference(cache, "RedisCache")
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("employee-wasm")
    .WaitFor(service);

builder.Build().Run();
