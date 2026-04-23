using Amazon;
using Aspire.Hosting.AWS;
using Aspire.Hosting.LocalStack;
using Aspire.Hosting.LocalStack.Container;
using LocalStack.Client.Enums;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var apiServiceConfig = builder.Configuration.GetSection("ApiService");
var ports = apiServiceConfig.GetSection("Ports").Get<int[]>() ?? [];

var apiGatewayConfig = builder.Configuration.GetSection("ApiGateway");
var gatewayPort = apiGatewayConfig.GetValue<int>("Port");

var localStackPort = builder.Configuration.GetSection("LocalStack").GetValue<int>("Port");
var cloudFormationTemplate = builder.Configuration.GetSection("LocalStack").GetValue<string>("CloudFormationTemplate") ?? "";
var snsEndpointUrl = builder.Configuration.GetSection("SNS").GetValue<string>("EndpointURL") ?? "";

var cache = builder.AddRedis("cache")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.Inventory_Gateway>("apigateway")
    .WithHttpsEndpoint(port: gatewayPort, name: "gateway")
    .WithExternalHttpEndpoints();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

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
    container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
}) ?? throw new InvalidOperationException("LocalStack resource could not be created.");

var awsResources = builder.AddAWSCloudFormationTemplate(
        "resources",
        cloudFormationTemplate,
        "inventory")
    .WithReference(awsConfig);

var storage = builder.AddProject<Projects.Inventory_FileService>("inventory-files")
    .WithReference(awsConfig)
    .WithReference(awsResources)
    .WithEnvironment("SNS__EndpointURL", snsEndpointUrl)
    .WaitFor(localstack)
    .WaitFor(awsResources);

var serviceId = 1;
foreach (var port in ports)
{
    var api = builder.AddProject<Projects.Inventory_ApiService>($"apiservice-{serviceId++}", launchProfileName: null)
        .WithReference(cache, "RedisCache")
        .WithHttpsEndpoint(port: port, name: "api-endpoint")
        .WithReference(awsConfig)
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(localstack)
        .WaitFor(cache)
        .WaitFor(storage);

    gateway.WaitFor(api);
}

// comment client nếu type project chưa đúng
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();