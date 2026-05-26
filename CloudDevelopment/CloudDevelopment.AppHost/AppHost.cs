var builder = DistributedApplication.CreateBuilder(args);

var redis = builder
    .AddRedis("redis")
    .WithRedisInsight();

// LocalStack
var localstack = builder.AddContainer("localstack", "localstack/localstack")
    .WithEndpoint("localstack", e =>
    {
        e.TargetPort = 4566;
        e.UriScheme = "http";
    })
    .WithEnvironment("SERVICES", "s3,sns,sqs")
    .WaitFor(redis);

var localstackEndpoint = localstack.GetEndpoint("localstack");

// Реплики генератора
var ports = new[] { 7130, 7131, 7132 };

var generationServices = ports
    .Select((port, index) =>
    {
        var name = $"generation-{index + 1}";
        return builder
            .AddProject<Projects.GenerationService>(name)
            .WithReference(redis)
            .WithHttpsEndpoint(port: port)
            .WithEnvironment("REPLICA_NAME", name)
            .WithEnvironment("AWS:ServiceURL", localstackEndpoint)
            .WaitFor(redis)
            .WaitFor(localstack);
    })
    .ToArray();

// FileService
var fileService = builder
    .AddProject<Projects.FileService>("file-service")
    .WithEndpoint("http", e => e.Port = 5300)
    .WithEnvironment("AWS:ServiceURL", localstackEndpoint)
    .WaitFor(localstack);

// Api Gateway
var gateway = builder
    .AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(fileService)
    .WaitForCompletion(generationServices[0])
    .WaitForCompletion(generationServices[1])
    .WaitForCompletion(generationServices[2])
    .WaitFor(fileService);

// Client
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();