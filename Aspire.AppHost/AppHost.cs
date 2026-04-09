var builder = DistributedApplication.CreateBuilder(args);
var redis = builder.AddRedis("cache").WithRedisInsight();

var generator0 = builder.AddProject<Projects.Server>("back-0")
    .WithEndpoint("https", endpoint => endpoint.Port = 8090)
    .WithReference(redis)
    .WaitFor(redis);

var generator1 = builder.AddProject<Projects.Server>("back-1")
    .WithEndpoint("https", endpoint => endpoint.Port = 8091)
    .WithReference(redis)
    .WaitFor(redis);

var generator2 = builder.AddProject<Projects.Server>("back-2")
    .WithEndpoint("https", endpoint => endpoint.Port = 8092)
    .WithReference(redis)
    .WaitFor(redis);

var gateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithEndpoint("https", endpoint => endpoint.Port = 8095)
    .WithReference(generator0)
    .WithReference(generator1)
    .WithReference(generator2)
    .WithExternalHttpEndpoints()
    .WaitFor(generator0)
    .WaitFor(generator1)
    .WaitFor(generator2);

builder.AddProject<Projects.Client_Wasm>("front")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();