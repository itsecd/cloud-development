using Aspire.Hosting.LocalStack.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var redis = builder.AddRedis("cache")
    .WithRedisCommander();

var localStackOptions = builder.AddLocalStackOptions()
    .WithUseLocalStack(true)
    .WithLocalStackHost("http://localhost:4566");

var localStack = builder.AddLocalStack("localstack", localStackOptions: localStackOptions, configureContainer: container =>
{
    container.AdditionalEnvironmentVariables["SERVICES"] = "sqs";
    container.Port = 4566;
});
builder.UseLocalStack(localStack);

var minioUser = builder.AddParameter("minio-user", "minioadmin", publishValueAsDefault: true, secret: false);
var minioPassword = builder.AddParameter("minio-password", "minioadmin", publishValueAsDefault: false, secret: true);
var minio = builder.AddMinioContainer("minio", minioUser, minioPassword, port: 9000);

var gateway = builder.AddProject<Projects.ProjectApp_Gateway>("projectapp-gateway");

var apiPorts = new[] { 5500, 5501, 5502 };
var weights = new[] { 3, 2, 1 };
var downstreamHosts = new List<string>();

var awsAccessKey = configuration["Aws:AccessKey"] ?? "test";
var awsSecretKey = configuration["Aws:SecretKey"] ?? "test";
var awsRegion = configuration["Aws:Region"] ?? "us-east-1";
var sqsServiceUrl = configuration["Sqs:ServiceUrl"] ?? "http://localhost:4566";
var sqsQueueName = configuration["Sqs:QueueName"] ?? "credit-application-generated";
var minioServiceUrl = configuration["Minio:ServiceUrl"] ?? "http://localhost:9000";
var minioAccessKey = configuration["Minio:AccessKey"] ?? "minioadmin";
var minioSecretKey = configuration["Minio:SecretKey"] ?? "minioadmin";
var minioBucketName = configuration["Minio:BucketName"] ?? "credit-applications";

for (var i = 0; i < apiPorts.Length; i++)
{
    var service = builder.AddProject<Projects.ProjectApp_Api>($"projectapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(apiPorts[i])
        .WithReference(redis)
        .WaitFor(redis)
        .WithEnvironment("Aws__AccessKey", awsAccessKey)
        .WithEnvironment("Aws__SecretKey", awsSecretKey)
        .WithEnvironment("Aws__Region", awsRegion)
        .WithEnvironment("Sqs__ServiceUrl", sqsServiceUrl)
        .WithEnvironment("Sqs__QueueName", sqsQueueName);

    if (localStack is not null)
    {
        service.WaitFor(localStack);
    }

    gateway.WithReference(service);
    gateway.WaitFor(service);

    downstreamHosts.Add($"localhost:{apiPorts[i]}:{weights[i]}");
}

gateway.WithEnvironment("DOWNSTREAM_HOSTS", string.Join(',', downstreamHosts));

var fileService = builder.AddProject<Projects.ProjectApp_FileService>("projectapp-file-service")
    .WaitFor(minio)
    .WithEnvironment("Aws__AccessKey", awsAccessKey)
    .WithEnvironment("Aws__SecretKey", awsSecretKey)
    .WithEnvironment("Aws__Region", awsRegion)
    .WithEnvironment("Sqs__ServiceUrl", sqsServiceUrl)
    .WithEnvironment("Sqs__QueueName", sqsQueueName)
    .WithEnvironment("Minio__ServiceUrl", minioServiceUrl)
    .WithEnvironment("Minio__AccessKey", minioAccessKey)
    .WithEnvironment("Minio__SecretKey", minioSecretKey)
    .WithEnvironment("Minio__BucketName", minioBucketName);

if (localStack is not null)
{
    fileService.WaitFor(localStack);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();
