var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

builder.AddProject<Projects.Client_Wasm>("client-wasm");

var generation1 = builder.AddProject<Projects.GenerationService>("generation-service-1")
    .WithReference(redis)
    .WaitFor(redis);

var generation2 = builder.AddProject<Projects.GenerationService>("generation-service-2")
    .WithReference(redis)
    .WaitFor(redis);

var generation3 = builder.AddProject<Projects.GenerationService>("generation-service-3")
    .WithReference(redis)
    .WaitFor(redis);

builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(generation1)
    .WithReference(generation2)
    .WithReference(generation3)
    .WaitFor(generation1)
    .WaitFor(generation2)
    .WaitFor(generation3);

builder.Build().Run();