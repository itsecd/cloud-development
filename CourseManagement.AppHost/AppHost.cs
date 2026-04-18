using Amazon;
using Aspire.Hosting.LocalStack.Container;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Read configuration for Api Service
var apiServiceConfig = builder.Configuration.GetSection("ApiService");
var ports = apiServiceConfig.GetSection("Ports").Get<List<int>>() ?? [];

// Read configuration for Api Gateway
var apiGatewayConfig = builder.Configuration.GetSection("ApiGateway");
var gatewayPort = apiGatewayConfig.GetValue<int>("Port");

// Read configuration for Localstack, SNS
var localStackPort = builder.Configuration.GetSection("LocalStack").GetValue<int>("Port");
var cloudFormationTemplate = builder.Configuration.GetSection("LocalStack").GetValue<string>("CloudFormationTemplate") ?? "";
var snsEndpointUrl = builder.Configuration.GetSection("SNS").GetValue<string>("EndpointURL") ?? "";

// Cache (Redis)
var redis = builder.AddRedis("course-cache")
    .WithRedisInsight(containerName: "course-insight")
    .WithDataVolume();

// API Gateway (Ocelot)
var apiGateway = builder.AddProject<Projects.CourseManagement_ApiGateway>("course-gateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "course-gateway-endpoint", isProxied: false)
    .WithExternalHttpEndpoints();

// AWS
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

// LocalStack (SNS + S3)
var localStack = builder
    .AddLocalStack("course-localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = localStackPort;
        container.AdditionalEnvironmentVariables
            .Add("DEBUG", "1");
        container.AdditionalEnvironmentVariables
            .Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
    });

// LocalStack Initialization
var awsResources = builder.AddAWSCloudFormationTemplate("course-resources", cloudFormationTemplate, "landplot")
    .WithReference(awsConfig);

// Storage Service (S3 + SNS)
var storage = builder.AddProject<Projects.CourseManagement_Storage>("course-storage")
    .WithReference(awsResources)
    .WithEnvironment("SNS__EndpointURL", snsEndpointUrl)
    .WaitFor(awsResources);


// API services (Backend)
var serviceId = 1;
foreach (var port in ports)
{
    var apiService = builder.AddProject<Projects.CourseManagement_ApiService>($"course-api-{serviceId++}")
        .WithReference(redis)
        .WithHttpsEndpoint(port: port, name: "course-api-endpoint", isProxied: false)
        .WithReference(awsResources)
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

builder.UseLocalStack(localStack);

builder.Build().Run();