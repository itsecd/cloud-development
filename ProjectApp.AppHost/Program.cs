using Aspire.Hosting.LocalStack.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");
builder.AddContainer("redis-commander", "rediscommander/redis-commander:latest")
    .WithEnvironment("REDIS_HOSTS", "local:cache:6379")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "http");

var localstackOptions = builder.AddLocalStackOptions()
    .WithUseLocalStack(true)
    .WithLocalStackHost("http://localhost:4566");

var localstack = builder.AddLocalStack("localstack", localStackOptions: localstackOptions, configureContainer: container =>
{
    container.AdditionalEnvironmentVariables["SERVICES"] = "sqs,s3";
    container.Port = 4566;
});
builder.UseLocalStack(localstack);

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 7000);

for (var i = 1; i <= 3; i++)
{
    var replica = builder.AddProject<Projects.ProjectApp_Api>($"projectapp-api-r{i}", launchProfileName: "http")
        .WithReference(cache)
        .WaitFor(cache)
        .WithEndpoint("http", endpoint => endpoint.Port = 7000 + i);

    if (localstack is not null)
    {
        replica.WithReference(localstack)
            .WaitFor(localstack);
    }

    gateway.WithReference(replica).WaitFor(replica);
}

var fileService = builder.AddProject<Projects.ProjectApp_FileService>("projectapp-file-service");
if (localstack is not null)
{
    fileService.WithReference(localstack)
        .WaitFor(localstack);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
