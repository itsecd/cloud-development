var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("residential-building-cache")
    .WithRedisInsight(containerName: "residential-building-insight");

var generator = builder.AddProject<Projects.ResidentialBuilding_Generator>("generator")
    .WithReference(cache, "residential-building-cache")
    .WaitFor(cache);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(generator)
    .WaitFor(generator);

builder.Build().Run();