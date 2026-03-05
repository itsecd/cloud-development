var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var generationService0 = builder.AddProject<Projects.GenerationService>("generation-service-0")
    .WithReference(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 5000)
    .WaitFor(cache);

var generationService1 = builder.AddProject<Projects.GenerationService>("generation-service-1")
    .WithReference(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 5001)
    .WaitFor(cache);

var generationService2 = builder.AddProject<Projects.GenerationService>("generation-service-2")
    .WithReference(cache)
    .WithEndpoint("http", endpoint => endpoint.Port = 5002)
    .WaitFor(cache);

var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 5100)
    .WithExternalHttpEndpoints()
    .WaitFor(generationService0)
    .WaitFor(generationService1)
    .WaitFor(generationService2);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();
