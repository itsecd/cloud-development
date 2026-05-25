var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var localStack = builder.AddContainer("localstack", "localstack/localstack", "latest")
    .WithEnvironment("SERVICES", "sqs")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "http");

var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console");

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway");

var apiPorts = new[] { 5500, 5501, 5502 };
var weights = new[] { 3, 2, 1 };
var downstreamHosts = new List<string>();

for (var i = 0; i < apiPorts.Length; i++)
{
    var service = builder.AddProject<Projects.ProjectApp_Api>($"projectapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(apiPorts[i])
        .WithReference(redis)
        .WaitFor(redis)
        .WaitFor(localStack)
        .WithEnvironment("Aws__AccessKey", "test")
        .WithEnvironment("Aws__SecretKey", "test")
        .WithEnvironment("Aws__Region", "us-east-1")
        .WithEnvironment("Sqs__ServiceUrl", "http://localhost:4566")
        .WithEnvironment("Sqs__QueueName", "credit-application-generated");

    gateway.WithReference(service);
    gateway.WaitFor(service);

    downstreamHosts.Add($"localhost:{apiPorts[i]}:{weights[i]}");
}

gateway.WithEnvironment("DOWNSTREAM_HOSTS", string.Join(',', downstreamHosts));

builder.AddProject<Projects.ProjectApp_FileService>("projectapp-file-service")
    .WaitFor(localStack)
    .WaitFor(minio)
    .WithEnvironment("Aws__AccessKey", "test")
    .WithEnvironment("Aws__SecretKey", "test")
    .WithEnvironment("Aws__Region", "us-east-1")
    .WithEnvironment("Sqs__ServiceUrl", "http://localhost:4566")
    .WithEnvironment("Sqs__QueueName", "credit-application-generated")
    .WithEnvironment("Minio__ServiceUrl", "http://localhost:9000")
    .WithEnvironment("Minio__AccessKey", "minioadmin")
    .WithEnvironment("Minio__SecretKey", "minioadmin")
    .WithEnvironment("Minio__BucketName", "credit-applications");

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
