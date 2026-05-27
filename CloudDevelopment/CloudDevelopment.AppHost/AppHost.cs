var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithRedisInsight();

var localstack = builder.AddContainer("localstack", "localstack/localstack:3.8.0")
    .WithEndpoint("localstack", e =>
    {
        e.TargetPort = 4566;
        e.UriScheme = "http";
    })
    .WithEnvironment("SERVICES", "s3,sns,sqs")
    .WithEnvironment("LOCALSTACK_AUTH_TOKEN", "")
    .WithEnvironment("DEBUG", "1")
    .WaitFor(redis);

var localstackEndpoint = localstack.GetEndpoint("localstack");

var fileService = builder.AddProject<Projects.FileService>("file-service")
    .WithEnvironment("AWS:ServiceURL", localstackEndpoint)
    .WaitFor(localstack);

// Generation Services
var generation1 = builder.AddProject<Projects.GenerationService>("generation-1")
    .WithReference(redis)
    .WithEnvironment("REPLICA_NAME", "generation-1")
    .WithEnvironment("AWS:ServiceURL", localstackEndpoint)
    .WaitFor(redis)
    .WaitFor(localstack)
    .WaitFor(fileService); 

var generation2 = builder.AddProject<Projects.GenerationService>("generation-2")
    .WithReference(redis)
    .WithEnvironment("REPLICA_NAME", "generation-2")
    .WithEnvironment("AWS:ServiceURL", localstackEndpoint)
    .WaitFor(redis)
    .WaitFor(localstack)
    .WaitFor(fileService);

var generation3 = builder.AddProject<Projects.GenerationService>("generation-3")
    .WithReference(redis)
    .WithEnvironment("REPLICA_NAME", "generation-3")
    .WithEnvironment("AWS:ServiceURL", localstackEndpoint)
    .WaitFor(redis)
    .WaitFor(localstack)
    .WaitFor(fileService);



// Api Gateway
var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(generation1)
    .WithReference(generation2)
    .WithReference(generation3)
    .WithReference(fileService)
    .WaitFor(generation1)
    .WaitFor(generation2)
    .WaitFor(generation3)
    .WaitFor(fileService)
    .WaitFor(localstack);

// Client
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();