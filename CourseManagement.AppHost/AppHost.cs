using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);


// Configuration Api Service
var apiServiceConfig = builder.Configuration.GetSection("ApiService");
var ports = apiServiceConfig.GetSection("Ports").Get<List<int>>() ?? [];

// Configuration Api Gateway
var apiGatewayConfig = builder.Configuration.GetSection("ApiGateway");
var gatewayPort = apiGatewayConfig.GetValue<int>("Port");

// Configuration AWS
var courseStack = builder.Configuration.GetSection("AWS").GetSection("Resources");
var topicName = courseStack.GetValue<string>("TopicName") ?? "course-topic";
var bucketName = courseStack.GetValue<string>("BucketName") ?? "course-bucket";
var snsTopicArn = courseStack.GetValue<string>("SNSTopicArn") ?? $"arn:aws:sns:eu-central-1:000000000000:{topicName}";
var snsSubscriberUrl = courseStack.GetValue<string>("SNSEndpointUrl") ?? "http://course-storage:5280/api/sns";

// Configuration S3
var s3Config = builder.Configuration.GetSection("S3");
var s3ServiceUrl = s3Config.GetValue<string>("ServiceURL") ?? "http://s3.localhost.localstack.cloud:4566";
var s3AccessKey = s3Config.GetValue<string>("AccessKeyId") ?? "test";
var s3SecretKey = s3Config.GetValue<string>("SecretAccessKey") ?? "test";
var s3Region = s3Config.GetValue<string>("Region") ?? "eu-central-1";

// Configuration SNS
var snsConfig = builder.Configuration.GetSection("SNS");
var snsServiceUrl = snsConfig.GetValue<string>("ServiceURL") ?? "http://sns.localhost.localstack.cloud:4566";
var snsAccessKey = snsConfig.GetValue<string>("AccessKeyId") ?? "test";
var snsSecretKey = snsConfig.GetValue<string>("SecretAccessKey") ?? "test";
var snsRegion = snsConfig.GetValue<string>("Region") ?? "eu-central-1";


// Cache (Redis)
var redis = builder.AddRedis("course-cache")
    .WithRedisInsight(containerName: "course-insight")
    .WithDataVolume();


// LocalStack (SNS + S3)
var localstack = builder.AddContainer("localstack", "localstack/localstack:3.8.0")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "localstack", scheme: "http")
    .WithEnvironment("SERVICES", "sns,s3")
    .WithEnvironment("AWS_ACCESS_KEY_ID", s3AccessKey)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", s3SecretKey)
    .WithEnvironment("AWS_DEFAULT_REGION", s3Region)
    .WithEnvironment("SKIP_SIGNATURE_VALIDATION", "1")
    .WithContainerRuntimeArgs(
        "--add-host", "host.docker.internal:host-gateway",
        "--add-host", "course-storage:host-gateway"
    );


// Storage service (S3 + SNS)
var storage = builder.AddProject<Projects.CourseManagement_Storage>("course-storage")
    .WithEnvironment("S3__ServiceURL", s3ServiceUrl)
    .WithEnvironment("S3__Region", s3Region)
    .WithEnvironment("S3__AccessKeyId", s3AccessKey)
    .WithEnvironment("S3__SecretAccessKey", s3SecretKey)
    .WithEnvironment("SNS__ServiceURL", snsServiceUrl)
    .WithEnvironment("SNS__Region", snsRegion)
    .WithEnvironment("SNS__AccessKeyId", snsAccessKey)
    .WithEnvironment("SNS__SecretAccessKey", snsSecretKey)
    .WithEnvironment("AWS__Resources__S3BucketName", bucketName)
    .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
    .WithEnvironment("AWS__Resources__SNSEndpointUrl", "http://host.docker.internal:5280/api/sns")
    .WithHttpEndpoint(port: 5280, name: "course-storage-endpoint")
    .WithExternalHttpEndpoints()
    .WaitFor(localstack);


var localstackInit = builder.AddContainer("localstack-init", "amazon/aws-cli")
    .WithArgs(
        "--endpoint-url=http://localstack:4566",
        "sns", "create-topic",
        "--name", topicName,
        "--region", snsRegion
    )
    .WithEnvironment("AWS_ACCESS_KEY_ID", snsAccessKey)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", snsSecretKey)
    .WithEnvironment("AWS_DEFAULT_REGION", snsRegion)
    .WaitFor(storage);


// API Gateway (Ocelot)
var apiGateway = builder.AddProject<Projects.CourseManagement_ApiGateway>("course-gateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "course-gateway-endpoint", isProxied: false)
    .WithExternalHttpEndpoints();


// API services (Backend)
var serviceId = 1;
foreach (var port in ports)
{
    var apiService = builder.AddProject<Projects.CourseManagement_ApiService>($"course-api-{serviceId++}")

        .WithReference(redis)
        .WithHttpsEndpoint(port: port, name: "course-api-endpoint", isProxied: false)
        .WithExternalHttpEndpoints()
        .WithEnvironment("SNS__ServiceURL", snsServiceUrl)
        .WithEnvironment("SNS__Region", snsRegion)
        .WithEnvironment("SNS__AccessKeyId", snsAccessKey)
        .WithEnvironment("SNS__SecretAccessKey", snsSecretKey)
        .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
        .WaitFor(redis)
        .WaitFor(localstack);

    apiGateway.WaitFor(apiService);
}

apiGateway.WithHttpHealthCheck("/health");


// Client (Frontend)
builder.AddProject<Projects.Client_Wasm>("course-wasm")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();