var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedis("project-cache")
    .WithRedisInsight(containerName: "project-insight");

var service = builder.AddProject<Projects.ServiceApi>("service-api")
    .WithReference(cache, "RedisCache")
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("programproject-wasm")
    .WaitFor(service);

builder.Build().Run();
