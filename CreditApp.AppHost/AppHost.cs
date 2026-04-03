var builder = DistributedApplication.CreateBuilder(args);

// Redis
var redis = builder.AddRedis("redis")
    .WithRedisCommander(containerName: "redis-commander");

// LocalStack (S3 + SQS)
var localstack = builder.AddContainer("localstack", "localstack/localstack:latest")
    .WithEnvironment("LOCALSTACK_AUTH_TOKEN", "ls-faLE5493-recA-BUha-5325-gifOqApu7f11") // <-- ТВОЙ ТОКЕН
    .WithEnvironment("SERVICES", "s3,sqs")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "api")
    .WithLifetime(ContainerLifetime.Persistent);

// Gateway
var gateway = builder.AddProject<Projects.CreditApp_Gateway>("gateway")
    .WithEndpoint("https", e =>
    {
        e.Port = 9002;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithExternalHttpEndpoints();

// 5 реплик API
for (var i = 0; i < 5; i++)
{
    var port = 7401 + i;
    var api = builder.AddProject<Projects.CreditApp_Api>($"api{i + 1}")
        .WithEndpoint("https", e =>
        {
            e.Port = port;
            e.IsProxied = false;
            e.UriScheme = "https";
        })
        .WithReference(redis)
        .WithEnvironment("LOCALSTACK_URL", "http://localhost:4566")
        .WaitFor(redis)
        .WaitFor(localstack);

    gateway.WaitFor(api);
}

// FileService
builder.AddProject<Projects.CreditApp_FileService>("fileservice")
    .WithEnvironment("LOCALSTACK_URL", "http://localhost:4566")
    .WaitFor(localstack);

// Клиент
builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();