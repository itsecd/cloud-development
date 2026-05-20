var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

// LocalStack поднимаем как контейнерный ресурс Aspire с фиксированным edge-портом,
// чтобы и процессы на хосте, и сами сервисы могли обращаться к нему по одному адресу.
var localstack = builder.AddContainer("localstack", "localstack/localstack", "3.5")
    .WithEnvironment("SERVICES", "s3,sns")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("DEBUG", "1")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "http", isProxied: false)
    .WithHttpHealthCheck("/_localstack/health", endpointName: "http");


const string localstackEndpoint = "http://localhost:4566";

var gateway = builder.AddProject<Projects.ApiGateway>("api-gateway");

var replicaWeights = new[] { 1, 2, 3, 2, 1 };

const int fileServicePort = 16000;

var fileService = builder.AddProject<Projects.File_Service>("file-service", launchProfileName: null)
    .WithHttpEndpoint(port: fileServicePort, name: "http")
    .WithEnvironment("Aws__ServiceUrl", localstackEndpoint)
    .WithEnvironment("Aws__Region", "us-east-1")
    .WithEnvironment("Aws__AccessKey", "test")
    .WithEnvironment("Aws__SecretKey", "test")
    .WithEnvironment("Aws__TopicName", "employee-generated-topic")
    .WithEnvironment("Aws__BucketName", "employee-files")
    .WaitFor(localstack);

// NotificationEndpoint собираем уже после описания endpoint, чтобы порт
// можно было резолвить динамически (важно для Aspire.Testing,
// где фиксированный 16000 может быть переопределён).
var fileServiceHttp = fileService.GetEndpoint("http");
fileService.WithEnvironment("Aws__NotificationEndpoint", ReferenceExpression.Create(
    $"http://host.docker.internal:{fileServiceHttp.Property(EndpointProperty.Port)}/sns/notifications"));

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 15000 + i)
        .WithReference(cache, "RedisCache")
        .WithEnvironment("ReplicaId", "R" + (i + 1))
        .WithEnvironment("ReplicaWeight", replicaWeights[i].ToString())
        .WithEnvironment("Aws__ServiceUrl", localstackEndpoint)
        .WithEnvironment("Aws__Region", "us-east-1")
        .WithEnvironment("Aws__AccessKey", "test")
        .WithEnvironment("Aws__SecretKey", "test")
        .WithEnvironment("Aws__TopicName", "employee-generated-topic")
        .WaitFor(cache)
        .WaitFor(localstack);

    gateway.WaitFor(service);
}

gateway.WaitFor(fileService);

builder.AddProject<Projects.Client_Wasm>("employee")
    .WaitFor(gateway);

builder.Build().Run();