var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
builder.AddContainer("redis-commander", "rediscommander/redis-commander:latest")
    .WithEnvironment("REDIS_HOSTS", "local:cache:6379")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "http");

var localstack = builder.AddContainer("localstack", "localstack/localstack:3.8")
    .WithEnvironment("SERVICES", "s3,sqs")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("DEBUG", "0")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "http");

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 7000);

for (var i = 1; i <= 3; i++)
{
    var replica = builder.AddProject<Projects.ProjectApp_Api>($"projectapp-api-r{i}")
        .WithReference(cache)
        .WaitFor(cache)
        .WaitFor(localstack)
        .WithEndpoint("http", endpoint => endpoint.Port = 7000 + i);

    gateway.WithReference(replica).WaitFor(replica);
}

builder.AddProject<Projects.ProjectApp_FileService>("projectapp-file-service")
    .WaitFor(localstack);

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
