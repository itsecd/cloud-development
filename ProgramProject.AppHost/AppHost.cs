var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithRedisCommander();

// Сервис генерации теперь делится на 3 реплики
var generationService = builder.AddProject<Projects.ProgramProject_GenerationService>("generation-service")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReplicas(3);

// Шлюз
var gateway = builder.AddProject<Projects.ProgramProject_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(generationService);

// Клиент теперь связывается с генератором через шлюз
var client = builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();