using Amazon;
using Aspire.Hosting.LocalStack.Container;
using LocalStack.Client.Enums;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Read configuration
var apiServiceConfig = builder.Configuration.GetSection("ApiService");
var ports = apiServiceConfig.GetSection("Ports").Get<List<int>>() ?? [7001, 7002, 7003, 7004, 7005];

var apiGatewayConfig = builder.Configuration.GetSection("ApiGateway");
var gatewayPort = apiGatewayConfig.GetValue<int>("Port");

var localStackPort = builder.Configuration.GetSection("LocalStack").GetValue<int>("Port");
var cloudFormationTemplate = builder.Configuration.GetSection("LocalStack").GetValue<string>("CloudFormationTemplate")
    ?? "CloudFormation/inventory-template-sns-s3.yaml";

var snsEndpointUrl = builder.Configuration.GetSection("SNS").GetValue<string>("EndpointURL")
    ?? "http://host.docker.internal:5037/api/sns";

// Cache Redis
var cache = builder.AddRedis("cache")
    .WithRedisCommander();

// API Gateway
var gateway = builder.AddProject<Projects.Inventory_Gateway>("apigateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "gateway")
    .WithExternalHttpEndpoints();

// AWS config
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

// LocalStack
var localstack = builder.AddLocalStack("inventory-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = localStackPort;

    container.EagerLoadedServices =
    [
        AwsService.CloudFormation,
        AwsService.S3,
        AwsService.Sns
    ];

    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    container.AdditionalEnvironmentVariables.Add("AWS_REGION", "eu-central-1");
    container.AdditionalEnvironmentVariables.Add("AWS_DEFAULT_REGION", "eu-central-1");
    container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
}) ?? throw new InvalidOperationException("LocalStack resource could not be created.");

// CloudFormation resources: S3 bucket + SNS topic
var awsResources = builder.AddAWSCloudFormationTemplate("resources",  cloudFormationTemplate, "inventory")
    .WithReference(awsConfig);

// FileService 
var fileService = builder.AddProject<Projects.Inventory_FileService>("inventory-files")
    .WithEnvironment("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "")
    .WithReference(awsResources)
    .WithEnvironment("Settings__MessageBroker", "SNS")
    .WithEnvironment("SNS__EndpointURL", snsEndpointUrl)
    .WithEnvironment("AWS_REGION", "eu-central-1")
    .WithEnvironment("AWS_DEFAULT_REGION", "eu-central-1")
    .WaitFor(awsResources);

// API services
var serviceId = 1;

foreach (var port in ports)
{
    var api = builder.AddProject<Projects.Inventory_ApiService>($"apiservice-{serviceId}", launchProfileName: null)
        .WithEnvironment("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "")
        .WithReference(cache)
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
        .WithEnvironment("AWS__Region", "eu-central-1")
        .WithEnvironment("AWS_REGION", "eu-central-1")
        .WithEnvironment("AWS_DEFAULT_REGION", "eu-central-1")
        .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
        .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
        .WaitFor(cache)
        .WaitFor(awsResources)
        .WaitFor(fileService)
        .WithHttpsEndpoint(port: port, name: $"api{serviceId}");

    gateway.WaitFor(api);
    serviceId++;
}

// Client
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();