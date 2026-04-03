using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var localstack = builder.AddContainer("localstack", "localstack/localstack")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "localstack", scheme: "http")
    .WithEnvironment("SERVICES", "sns,sqs")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithHttpHealthCheck(path: "/_localstack/health", endpointName: "localstack");

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "api", scheme: "http")
    .WithEndpoint(port: 9001, targetPort: 9001, name: "console", scheme: "http")
    .WithHttpHealthCheck(path: "/minio/health/ready", endpointName: "api");

var fileService = builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WithReference(localstack.GetEndpoint("localstack"))
    .WaitFor(localstack)
    .WithReference(minio.GetEndpoint("api"))
    .WaitFor(minio)
    .WithEnvironment("AWS__ServiceURL", localstack.GetEndpoint("localstack"))
    .WithEnvironment("STORAGE__Endpoint", minio.GetEndpoint("api"));

var gateway = builder.AddProject<Projects.CompanyEmployee_Gateway>("gateway")
    .WithEndpoint("https", e => e.Port = 7000)
    .WithExternalHttpEndpoints();

const int startApiPort = 6001;
const int replicaCount = 5;
var apiReplicas = new List<IResourceBuilder<ProjectResource>>();

for (var i = 0; i < replicaCount; i++)
{
    var port = startApiPort + i;
    var api = builder.AddProject<Projects.CompanyEmployee_Api>($"api-{i + 1}")
        .WithReference(redis)
        .WaitFor(redis)
        .WithReference(localstack.GetEndpoint("localstack"))
        .WaitFor(localstack)
        .WaitFor(fileService)
        .WithEndpoint("https", e => e.Port = port)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("AWS__ServiceURL", localstack.GetEndpoint("localstack"))
        .WithEnvironment("SNS__TopicArn", "arn:aws:sns:us-east-1:000000000000:employee-events")
        .WithEnvironment("STORAGE__Endpoint", minio.GetEndpoint("api"))
        .WithEnvironment("STORAGE__AccessKey", "minioadmin")
        .WithEnvironment("STORAGE__AccessSecret", "minioadmin")
        .WithEnvironment("STORAGE__BucketName", "employee-data");

    apiReplicas.Add(api);
    gateway.WaitFor(api);
}

foreach (var replica in apiReplicas)
{
    gateway.WithReference(replica);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();