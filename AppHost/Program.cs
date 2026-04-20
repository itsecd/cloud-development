var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var localstack = builder.AddContainer("localstack", "localstack/localstack", "3.8.1")
    .WithEnvironment("SERVICES", "sns,sqs")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "http");

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console");

var client = builder.AddProject<Projects.Client_Wasm>("client");

var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithEnvironment("Cors__AllowedOrigin", client.GetEndpoint("http"));

for (var i = 0; i < 3; i++)
{
    var replica = builder.AddProject<Projects.GeneratorService>($"generator-service-{i}", launchProfileName: null)
        .WithHttpEndpoint(port: 15000 + i)
        .WithReference(redis)
        .WaitFor(redis)
        .WithEnvironment("AWS__ServiceURL", localstack.GetEndpoint("http"));
    gateway.WithReference(replica).WaitFor(replica);
}

builder.AddProject<Projects.FileService>("file-service")
    .WithHttpEndpoint(port: 5300, name: "http")
    .WithEnvironment("AWS__ServiceURL", localstack.GetEndpoint("http"))
    .WithEnvironment("Minio__ServiceUrl", minio.GetEndpoint("api"));

builder.Build().Run();
