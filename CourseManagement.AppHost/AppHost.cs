using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Read configuration for Api Service
var apiServiceConfig = builder.Configuration.GetSection("ApiService");
var ports = apiServiceConfig.GetSection("Ports").Get<List<int>>() ?? [];

// Read configuration for Api Gateway
var apiGatewayConfig = builder.Configuration.GetSection("ApiGateway");
var gatewayPort = apiGatewayConfig.GetValue<int>("Port");

// Read configuration for AWS
var awsConfig = builder.Configuration.GetSection("AWS");
var accessKey = awsConfig.GetValue<string>("AccessKeyId") ?? "test";
var secretKey = awsConfig.GetValue<string>("SecretAccessKey") ?? "test";
var region = awsConfig.GetValue<string>("Region") ?? "eu-central-1";

// Read configuration for AWS Resources
var awsResourcesConfig = awsConfig.GetSection("Resources");
var topicName = awsResourcesConfig.GetValue<string>("TopicName") ?? "course-topic";
var bucketName = awsResourcesConfig.GetValue<string>("BucketName") ?? "course-bucket";
var snsTopicArn = awsResourcesConfig.GetValue<string>("SNSTopicArn") ?? $"arn:aws:sns:eu-central-1:000000000000:{topicName}";
var snsEndpointUrl = awsResourcesConfig.GetValue<string>("SNSEndpointURL") ?? "http://course-storage:5280/api/sns";

// Read configuration for Localstack, S3, SNS
var localStackServiceUrl = builder.Configuration.GetSection("LocalStack").GetValue<string>("ServiceURL") ?? "http://localstack:4566";
var s3ServiceUrl = builder.Configuration.GetSection("S3").GetValue<string>("ServiceURL") ?? "http://s3.localhost.localstack.cloud:4566";
var snsServiceUrl = builder.Configuration.GetSection("SNS").GetValue<string>("ServiceURL") ?? "http://sns.localhost.localstack.cloud:4566";

// Cache (Redis)
var redis = builder.AddRedis("course-cache")
    .WithRedisInsight(containerName: "course-insight")
    .WithDataVolume();

// LocalStack (SNS + S3)
var localstack = builder.AddContainer("localstack", "localstack/localstack:3.8.0")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "localstack", scheme: "http")
    .WithEnvironment("SERVICES", "sns,s3")
    .WithEnvironment("AWS_ACCESS_KEY_ID", accessKey)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", secretKey)
    .WithEnvironment("AWS_DEFAULT_REGION", region)
    .WithEnvironment("SKIP_SIGNATURE_VALIDATION", "1")
    .WithContainerRuntimeArgs(
        "--add-host", "host.docker.internal:host-gateway"
    );

// LocalStack Initialization
var localstackInit = builder.AddContainer("localstack-init", "amazon/aws-cli")
    .WithArgs(
        $"--endpoint-url={localStackServiceUrl}",
        "sns", "create-topic",
        "--name", topicName,
        "--region", region
    )
    .WithEnvironment("AWS_ACCESS_KEY_ID", accessKey)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", secretKey)
    .WithEnvironment("AWS_DEFAULT_REGION", region)
    .WaitFor(localstack);

// Storage Service (S3 + SNS)
var storage = builder.AddProject<Projects.CourseManagement_Storage>("course-storage")
    .WithHttpEndpoint(port: 5280, name: "course-storage-endpoint")
    .WithEnvironment("AWS__Region", region)
    .WithEnvironment("AWS__AccessKeyId", accessKey)
    .WithEnvironment("AWS__SecretAccessKey", secretKey)
    .WithEnvironment("AWS__Resources__S3BucketName", bucketName)
    .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
    .WithEnvironment("AWS__Resources__SNSEndpointURL", "http://host.docker.internal:5280/api/sns")
    .WithEnvironment("S3__ServiceURL", s3ServiceUrl)
    .WithEnvironment("SNS__ServiceURL", snsServiceUrl)
    .WaitFor(localstackInit);

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
        .WithEnvironment("AWS__Region", region)
        .WithEnvironment("AWS__AccessKeyId", accessKey)
        .WithEnvironment("AWS__SecretAccessKey", secretKey)
        .WithEnvironment("AWS__Resources__SNSTopicArn", snsTopicArn)
        .WaitFor(redis)
        .WaitFor(storage);

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