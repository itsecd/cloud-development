var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
builder.AddContainer("redis-commander", "rediscommander/redis-commander:latest")
    .WithEnvironment("REDIS_HOSTS", "local:cache:6379")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "http");

var localstack = builder.AddLocalStack("localstack");
builder.UseLocalStack(localstack);

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 7000);

for (var i = 1; i <= 3; i++)
{
    var replica = builder.AddProject<Projects.ProjectApp_Api>($"projectapp-api-r{i}")
        .WithReference(cache)
        .WaitFor(cache)
        .WithEndpoint("http", endpoint => endpoint.Port = 7000 + i);

    gateway.WithReference(replica).WaitFor(replica);
}

builder.AddProject<Projects.ProjectApp_FileService>("projectapp-file-service");

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
