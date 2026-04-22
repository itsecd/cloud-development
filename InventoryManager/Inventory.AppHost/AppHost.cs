using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

// Redis
var cache = builder.AddRedis("cache")
    .WithRedisCommander();

// Gateway
var gateway = builder.AddProject<Projects.Inventory_Gateway>("apigateway")
    .WithHttpsEndpoint(port: 7000, name: "gateway");

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
    container.Port = 4566;
    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
});

// SNS + S3 resources
var awsResources = builder.AddAWSCloudFormationTemplate(
        "resources",
        "CloudFormation/inventory-template-sns-s3.yaml",
        "inventory")
    .WithReference(awsConfig);

// API replicas
var apis = new List<IResourceBuilder<ProjectResource>>();
var basePort = 7001;

for (var i = 0; i < 5; i++)
{
    var port = basePort + i;

    var api = builder.AddProject<Projects.Inventory_ApiService>($"apiservice-{i + 1}", launchProfileName: null)
        .WithReference(cache, "RedisCache")
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(cache)
        .WaitFor(awsResources)
        .WithHttpsEndpoint(port: port, name: $"api{i + 1}");

    apis.Add(api);
    gateway.WaitFor(api);
}

// Client
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(gateway);

// FileService / Sink
builder.AddProject<Projects.Inventory_FileService>("inventory-files")
    .WithReference(awsResources)
    .WithEnvironment("Settings__MessageBroker", "SNS")
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns")
    .WaitFor(awsResources);

builder.UseLocalStack(localstack);

builder.Build().Run();