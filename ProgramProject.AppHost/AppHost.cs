var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var generationService = builder.AddProject<Projects.ProgramProject_GenerationService>("generation-service")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache);

// Добавил клиента
var client = builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WithReference(generationService);

builder.Build().Run();
