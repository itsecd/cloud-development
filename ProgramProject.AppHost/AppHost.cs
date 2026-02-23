var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var generationService = builder.AddProject<Projects.ProgramProject_GenerationService>("generation-service")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();
