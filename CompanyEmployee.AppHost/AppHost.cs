var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithRedisInsight();

var generator1 = builder.AddProject<Projects.CompanyEmployee_ApiService>("generator-1")
    .WithHttpEndpoint(name: "http1", port: 5213)
    .WithReference(redis)
    .WaitFor(redis);

var generator2 = builder.AddProject<Projects.CompanyEmployee_ApiService>("generator-2")
    .WithHttpEndpoint(name: "http1", port: 5214)
    .WithReference(redis)
    .WaitFor(redis);

var generator3 = builder.AddProject<Projects.CompanyEmployee_ApiService>("generator-3")
    .WithHttpEndpoint(name: "http1", port: 5215)
    .WithReference(redis)
    .WaitFor(redis);



var api = builder.AddProject<Projects.CompanyEmployee_ApiGateway>("companyemployee-apigateway")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(generator1)
    .WithReference(generator2)
    .WithReference(generator3)
    .WithHttpEndpoint(name: "gateway", port: 5212)
    .WaitFor(generator1)
    .WaitFor(generator2)
    .WaitFor(generator3);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();