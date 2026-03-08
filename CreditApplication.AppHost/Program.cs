using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

if (builder.Environment.IsDevelopment())
    redis.WithRedisCommander();

var localstack = builder.AddContainer("localstack", "localstack/localstack")
    .WithEndpoint("localstack", e =>
    {
        e.TargetPort = 4566;
        e.UriScheme = "http";
    })
    .WithEnvironment("SERVICES", "s3,sns,sqs")
    .WaitFor(redis);

var localstackEndpoint = localstack.GetEndpoint("localstack");

var generator1 = builder.AddProject<Projects.CreditApplication_Generator>("generator-1")
    .WithEndpoint("http", endpoint => endpoint.Port = 5101)
    .WithReference(redis)
    .WithEnvironment("AWS__ServiceURL", localstackEndpoint)
    .WaitFor(redis)
    .WaitFor(localstack);

var generator2 = builder.AddProject<Projects.CreditApplication_Generator>("generator-2")
    .WithEndpoint("http", endpoint => endpoint.Port = 5102)
    .WithReference(redis)
    .WithEnvironment("AWS__ServiceURL", localstackEndpoint)
    .WaitFor(redis)
    .WaitFor(localstack);

var generator3 = builder.AddProject<Projects.CreditApplication_Generator>("generator-3")
    .WithEndpoint("http", endpoint => endpoint.Port = 5103)
    .WithReference(redis)
    .WithEnvironment("AWS__ServiceURL", localstackEndpoint)
    .WaitFor(redis)
    .WaitFor(localstack);

var fileService = builder.AddProject<Projects.CreditApplication_FileService>("file-service")
    .WithEndpoint("http", endpoint => endpoint.Port = 5300)
    .WithEnvironment("AWS__ServiceURL", localstackEndpoint)
    .WaitFor(localstack);

var gateway = builder.AddProject<Projects.CreditApplication_Gateway>("gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 5200)
    .WithReference(generator1)
    .WithReference(generator2)
    .WithReference(generator3)
    .WithReference(fileService)
    .WithExternalHttpEndpoints()
    .WaitFor(generator1)
    .WaitFor(generator2)
    .WaitFor(generator3)
    .WaitFor(fileService);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();
