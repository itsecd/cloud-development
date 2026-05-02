using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var apiPorts = builder.Configuration.GetSection("ApiService:Ports").Get<int[]>()
              ?? throw new InvalidOperationException("ApiService:Ports is not configured.");

var gatewayPort = builder.Configuration.GetValue<int?>("Gateway:Port")
                 ?? throw new InvalidOperationException("Gateway:Port is not configured.");

var localStackPort = builder.Configuration.GetValue<int?>("LocalStack:Port")
                     ?? throw new InvalidOperationException("LocalStack:Port is not configured.");

var snsServiceUrl = builder.Configuration["LocalStack:ServiceUrl"]
                    ?? throw new InvalidOperationException("LocalStack:ServiceUrl is not configured.");

var snsTopicArn = builder.Configuration["SNS:TopicArn"]
                  ?? throw new InvalidOperationException("SNS:TopicArn is not configured.");

var snsEndpointUrl = builder.Configuration["SNS:EndpointUrl"]
                     ?? throw new InvalidOperationException("SNS:EndpointUrl is not configured.");

var minioApiPort = builder.Configuration.GetValue<int?>("Minio:ApiPort")
                   ?? throw new InvalidOperationException("Minio:ApiPort is not configured.");

var minioConsolePort = builder.Configuration.GetValue<int?>("Minio:ConsolePort")
                       ?? throw new InvalidOperationException("Minio:ConsolePort is not configured.");

var minioEndpoint = builder.Configuration["Minio:Endpoint"]
                    ?? throw new InvalidOperationException("Minio:Endpoint is not configured.");

var minioAccessKey = builder.Configuration["Minio:AccessKey"]
                     ?? throw new InvalidOperationException("Minio:AccessKey is not configured.");

var minioSecretKey = builder.Configuration["Minio:SecretKey"]
                     ?? throw new InvalidOperationException("Minio:SecretKey is not configured.");

var minioBucketName = builder.Configuration["Minio:BucketName"]
                      ?? throw new InvalidOperationException("Minio:BucketName is not configured.");

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var localstack = builder.AddContainer("localstack", "localstack/localstack:3")
    .WithEnvironment("SERVICES", "sns")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("DEBUG", "1")
    .WithEnvironment("HOSTNAME_EXTERNAL", "host.docker.internal")
    .WithContainerRuntimeArgs("--add-host", "host.docker.internal:host-gateway")
    .WithHttpEndpoint(port: localStackPort, targetPort: 4566, name: "edge");

var minio = builder.AddContainer("minio", "minio/minio")
    .WithEnvironment("MINIO_ROOT_USER", minioAccessKey)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioSecretKey)
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(port: minioApiPort, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: minioConsolePort, targetPort: 9001, name: "console");

var eventSink = builder.AddProject<Projects.Vehicle_EventSink>(
        "vehicle-event-sink",
        launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:5303")
    .WithHttpEndpoint(
        port: 5303,
        targetPort: 5303,
        name: "event-sink-http",
        isProxied: false)
    .WithEnvironment("AWS__Region", "us-east-1")
    .WithEnvironment("AWS__ServiceUrl", snsServiceUrl)
    .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5303/api/sns")
    .WithEnvironment("AWS__Resources__MinioEndpoint", "localhost:9000")
    .WithEnvironment("AWS__Resources__MinioAccessKey", "minioadmin")
    .WithEnvironment("AWS__Resources__MinioSecretKey", "minioadmin")
    .WithEnvironment("AWS__Resources__MinioBucketName", "vehicle-files")
    .WaitFor(localstack)
    .WaitFor(minio);

var gateway = builder.AddProject<Projects.Vehicle_Gateway>("vehicle-gateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "vehicle-gateway-lb")
    .WaitFor(redis);

for (var i = 0; i < apiPorts.Length; i++)
{
    var httpsPort = apiPorts[i];
    var instanceName = $"vehicle-api-{i + 1}";

    var api = builder.AddProject<Projects.Vehicle_Api>(
            $"vehicle-api-{i + 1}",
            launchProfileName: null)
        .WithReference(redis)
        .WithHttpsEndpoint(port: httpsPort, name: instanceName)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("INSTANCE_ID", instanceName)
        .WithEnvironment("AWS__Region", "us-east-1")
        .WithEnvironment("AWS__ServiceUrl", snsServiceUrl)
        .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
        .WaitFor(redis)
        .WaitFor(localstack)
        .WaitFor(eventSink);

    gateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();