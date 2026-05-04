using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

const string awsRegion = "us-east-1";

var apiPorts = RequiredIntArray("ApiService:Ports");
var gatewayPort = RequiredInt("Gateway:Port");

var localStackPort = RequiredInt("LocalStack:Port");
var localStackServiceUrl = RequiredString("LocalStack:ServiceUrl");

var snsTopicArn = RequiredString("SNS:TopicArn");
var snsEndpointUrl = RequiredString("SNS:EndpointUrl");

var eventSinkPort = RequiredInt("EventSink:Port");
var eventSinkUrls = RequiredString("EventSink:Urls");

var minioApiPort = RequiredInt("Minio:ApiPort");
var minioConsolePort = RequiredInt("Minio:ConsolePort");
var minioEndpoint = RequiredString("Minio:Endpoint");
var minioAccessKey = RequiredString("Minio:AccessKey");
var minioSecretKey = RequiredString("Minio:SecretKey");
var minioBucketName = RequiredString("Minio:BucketName");

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var localstack = builder.AddContainer("localstack", "localstack/localstack:3")
    .WithEnvironment("SERVICES", "sns")
    .WithEnvironment("AWS_DEFAULT_REGION", awsRegion)
    .WithContainerRuntimeArgs("--add-host", "host.docker.internal:host-gateway")
    .WithHttpEndpoint(port: localStackPort, targetPort: 4566, name: "edge");

var minio = builder.AddContainer("minio", "minio/minio")
    .WithEnvironment("MINIO_ROOT_USER", minioAccessKey)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioSecretKey)
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(port: minioApiPort, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: minioConsolePort, targetPort: 9001, name: "console");

var eventSink = builder.AddProject<Projects.Vehicle_EventSink>("vehicle-event-sink", launchProfileName: null)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ASPNETCORE_URLS", eventSinkUrls)
    .WithHttpEndpoint(port: eventSinkPort, targetPort: eventSinkPort, name: "event-sink-http", isProxied: false)
    .WithEnvironment("AWS__Region", awsRegion)
    .WithEnvironment("AWS__ServiceUrl", localStackServiceUrl)
    .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
    .WithEnvironment("AWS__Resources__SNSUrl", snsEndpointUrl)
    .WithEnvironment("AWS__Resources__MinioEndpoint", minioEndpoint)
    .WithEnvironment("AWS__Resources__MinioAccessKey", minioAccessKey)
    .WithEnvironment("AWS__Resources__MinioSecretKey", minioSecretKey)
    .WithEnvironment("AWS__Resources__MinioBucketName", minioBucketName)
    .WaitFor(localstack)
    .WaitFor(minio);

var gateway = builder.AddProject<Projects.Vehicle_Gateway>("vehicle-gateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "vehicle-gateway-lb")
    .WaitFor(redis);

for (var i = 0; i < apiPorts.Length; i++)
{
    var instanceName = $"vehicle-api-{i + 1}";

    var api = builder.AddProject<Projects.Vehicle_Api>(instanceName, launchProfileName: null)
        .WithReference(redis)
        .WithHttpsEndpoint(port: apiPorts[i], name: instanceName)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("INSTANCE_ID", instanceName)
        .WithEnvironment("AWS__Region", awsRegion)
        .WithEnvironment("AWS__ServiceUrl", localStackServiceUrl)
        .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
        .WaitFor(redis)
        .WaitFor(localstack)
        .WaitFor(eventSink);

    gateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.Build().Run();

string RequiredString(string key)
{
    return builder.Configuration[key] ?? throw new InvalidOperationException($"{key} is not configured.");
}

int RequiredInt(string key)
{
    return builder.Configuration.GetValue<int?>(key) ?? throw new InvalidOperationException($"{key} is not configured.");
}

int[] RequiredIntArray(string key)
{
    return builder.Configuration.GetSection(key).Get<int[]>() ?? throw new InvalidOperationException($"{key} is not configured.");
}