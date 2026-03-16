var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "credit-redis-insight");

var softwareProjectsApi = builder.AddProject<Projects.SoftwareProjects_Api>("softwareprojects-api")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(softwareProjectsApi);

builder.Build().Run();
