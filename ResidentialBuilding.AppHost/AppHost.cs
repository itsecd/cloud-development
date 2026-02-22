using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<RedisResource> cache = builder.AddRedis("residential-building-cache")
    .WithRedisInsight(containerName: "residential-building-insight");

IResourceBuilder<ProjectResource> generator = builder.AddProject<ResidentialBuilding_Generator>("generator")
    .WithReference(cache, "residential-building-cache")
    .WaitFor(cache);

IResourceBuilder<ProjectResource> client = builder.AddProject<Client_Wasm>("client")
    .WithReference(generator)
    .WaitFor(generator);

builder.Build().Run();