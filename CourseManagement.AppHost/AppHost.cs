using Amazon;
using Aspire.Hosting.LocalStack.Container;
using Microsoft.Extensions.Configuration;
using CourseManagement.AppHost.Stacks;

var builder = DistributedApplication.CreateBuilder(args);


// Configuration
var apiServiceConfig = builder.Configuration.GetSection("ApiService");
var ports = apiServiceConfig.GetSection("Ports").Get<List<int>>() ?? [];

var apiGatewayConfig = builder.Configuration.GetSection("ApiGateway");
var gatewayPort = apiGatewayConfig.GetValue<int>("Port");

var topicName = builder.Configuration.GetSection("CourseStack").GetValue<string>("TopicName") ?? "course-topic";
var bucketName = builder.Configuration.GetSection("CourseStack").GetValue<string>("BucketName") ?? "course-bucket";


// Cache (Redis)
var redis = builder.AddRedis("course-cache")
    .WithRedisInsight(containerName: "course-insight")
    .WithDataVolume();


// Message Broker + Object Store (Localstack, SNS, S3)
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("course-localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables
            .Add("DEBUG", "1");
        container.AdditionalEnvironmentVariables
            .Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
    });

var awsResources = builder.AddAWSCDKStack("course-resources", stack => 
    new CourseStack(stack, "course-stack", new CourseStackProps
    {
        BucketName = bucketName,
        TopicName = topicName,
    }))
    .WithReference(awsConfig);


// API Gateway (Ocelot)
var apiGateway = builder.AddProject<Projects.CourseManagement_ApiGateway>("course-gateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "course-gateway-endpoint", isProxied: false)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");


// API services (Backend)
var apiServices = new List<IResourceBuilder<ProjectResource>>();

var serviceId = 1;
foreach(var port in ports)
{
    var apiService = builder.AddProject<Projects.CourseManagement_ApiService>($"course-api-{serviceId++}")
        .WithReference(redis)
        .WithHttpHealthCheck("/health")
        .WithHttpsEndpoint(port: port, name: "course-api-endpoint", isProxied: false)
        .WithExternalHttpEndpoints();
    apiServices.Add(apiService);

    apiGateway.WaitFor(apiService);
}


// Client (Frontend)
builder.AddProject<Projects.Client_Wasm>("course-wasm")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiGateway)
    .WaitFor(apiGateway);


builder.UseLocalStack(localstack);

builder.Build().Run();
