using Amazon;
using Aspire.Hosting.AWS;
using Aspire.Hosting.LocalStack;
using Aspire.Hosting.LocalStack.Container;
using LocalStack.Client.Enums;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.Inventory_Gateway>("apigateway")
    .WithHttpsEndpoint(port: 7000, name: "gateway")
    .WithExternalHttpEndpoints();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack("inventory-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;

    container.EagerLoadedServices =
    [
        AwsService.CloudFormation,
        AwsService.S3,
        AwsService.Sns
    ];

    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
}) ?? throw new InvalidOperationException("LocalStack resource could not be created.");

var awsResources = builder.AddAWSCloudFormationTemplate(
        "resources",
        "CloudFormation/inventory-template-sns-s3.yaml",
        "inventory")
    .WithReference(localstack)
    .WithReference(awsConfig);

var apis = new List<IResourceBuilder<ProjectResource>>();
var basePort = 7001;

for (var i = 0; i < 5; i++)
{
    var port = basePort + i;

    var api = builder.AddProject<Projects.Inventory_ApiService>($"apiservice-{i + 1}", launchProfileName: null)
        .WithReference(cache, "RedisCache")
        .WithReference(localstack)
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(cache)
        .WaitFor(localstack)
        .WaitFor(awsResources)
        .WithHttpsEndpoint(port: port, name: $"api{i + 1}");

    apis.Add(api);
    gateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(gateway);

builder.AddProject<Projects.Inventory_FileService>("inventory-files")
    .WithReference(localstack)
    .WithReference(awsResources)
    .WithEnvironment("Settings__MessageBroker", "SNS")
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns")
    .WaitFor(localstack)
    .WaitFor(awsResources);

builder.UseLocalStack(localstack);

builder.Build().Run();