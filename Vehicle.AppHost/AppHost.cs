using Vehicle.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

const string awsRegion = "us-east-1";

var apiPorts = builder.Configuration.GetRequiredIntArray("ApiService:Ports");
var gatewayPort = builder.Configuration.GetRequiredInt("Gateway:Port");

var localStackPort = builder.Configuration.GetRequiredInt("LocalStack:Port");
var localStackServiceUrl = builder.Configuration.GetRequiredString("LocalStack:ServiceUrl");

var snsTopicArn = builder.Configuration.GetRequiredString("SNS:TopicArn");
var snsEndpointUrl = builder.Configuration.GetRequiredString("SNS:EndpointUrl");

var eventSinkPort = builder.Configuration.GetRequiredInt("EventSink:Port");
var eventSinkUrls = builder.Configuration.GetRequiredString("EventSink:Urls");

var minioApiPort = builder.Configuration.GetRequiredInt("Minio:ApiPort");
var minioConsolePort = builder.Configuration.GetRequiredInt("Minio:ConsolePort");
var minioEndpoint = builder.Configuration.GetRequiredString("Minio:Endpoint");
var minioAccessKey = builder.Configuration.GetRequiredString("Minio:AccessKey");
var minioSecretKey = builder.Configuration.GetRequiredString("Minio:SecretKey");
var minioBucketName = builder.Configuration.GetRequiredString("Minio:BucketName");

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