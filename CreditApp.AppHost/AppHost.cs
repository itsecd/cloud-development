var builder = DistributedApplication.CreateBuilder(args);

var localstackToken = builder.Configuration["LocalStack:AuthToken"];

var redis = builder.AddRedis("redis")
    .WithRedisCommander(containerName: "redis-commander");

var localstack = builder.AddContainer("localstack", "localstack/localstack:latest")
    .WithEnvironment("LOCALSTACK_AUTH_TOKEN", localstackToken)
    .WithEnvironment("SERVICES", "s3,sqs")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "api")
    .WithLifetime(ContainerLifetime.Persistent);

var gateway = builder.AddProject<Projects.CreditApp_Gateway>("gateway")
    .WithEndpoint("https", e =>
    {
        e.Port = 9002;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithExternalHttpEndpoints();

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

builder.AddProject<Projects.CreditApp_FileService>("fileservice")
    .WithEnvironment("LOCALSTACK_URL", "http://localhost:4566")
    .WaitFor(localstack);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();