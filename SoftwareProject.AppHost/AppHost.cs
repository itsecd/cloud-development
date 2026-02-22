var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var generationService = builder.AddProject<Projects.SoftwareProject_GenerationService>("generation-service")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.SoftwareProject_GenerationService>("softwareproject-generationservice");

builder.Build().Run();
