using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Redis для кэширования
var redis = builder.AddRedis("redis")
    .WithRedisCommander();

// LocalStack (как контейнер)
var localstack = builder.AddContainer("localstack", "localstack/localstack", "latest")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "localstack-gateway", scheme: "http")
    .WithEnvironment("SERVICES", "sns,sqs")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WithEnvironment("DOCKER_HOST", "unix:///var/run/docker.sock");

// Data Generator сервис
var dataGenerator = builder.AddProject<Projects.DataGenerator>("datagenerator")
    .WithReference(localstack.GetEndpoint("localstack-gateway"));

// API Server
var apiServer = builder.AddProject<Projects.ApiServer>("apiserver")
    .WithReference(redis)
    .WithReference(localstack.GetEndpoint("localstack-gateway"));

builder.Build().Run();
