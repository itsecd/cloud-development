var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithRedisCommander();

var generationService = builder.AddProject<Projects.ProgramProject_GenerationService>("generation-service")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache);


// Добавил клиента
var client = builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(generationService);

builder.Build().Run();
