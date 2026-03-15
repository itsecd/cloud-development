var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithRedisCommander();

// Сервис генерации теперь делится на 5 реплик
var generator1 = builder.AddProject<Projects.ProgramProject_GenerationService>("generator-1")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 6201)
    .WithEndpoint("https", endpoint => endpoint.Port = 7201);

var generator2 = builder.AddProject<Projects.ProgramProject_GenerationService>("generator-2")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 6202)
    .WithEndpoint("https", endpoint => endpoint.Port = 7202);

var generator3 = builder.AddProject<Projects.ProgramProject_GenerationService>("generator-3")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 6203)
    .WithEndpoint("https", endpoint => endpoint.Port = 7203);

var generator4 = builder.AddProject<Projects.ProgramProject_GenerationService>("generator-4")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 6204)
    .WithEndpoint("https", endpoint => endpoint.Port = 7204);

var generator5 = builder.AddProject<Projects.ProgramProject_GenerationService>("generator-5")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 6205)
    .WithEndpoint("https", endpoint => endpoint.Port = 7205);
    
// Шлюз
var gateway = builder.AddProject<Projects.ProgramProject_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithReference(generator1)
    .WithReference(generator2)
    .WithReference(generator3)
    .WithReference(generator4)
    .WithReference(generator5)
    .WaitFor(generator1)
    .WaitFor(generator2)
    .WaitFor(generator3)
    .WaitFor(generator4)
    .WaitFor(generator5);

// Клиент теперь связывается с генератором через шлюз
var client = builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();