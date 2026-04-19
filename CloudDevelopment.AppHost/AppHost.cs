using Amazon;
using Amazon.CDK.AWS.Servicecatalog;
using Aspire.Hosting.LocalStack.Container;
using LocalStack.Client.Enums;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var ports = builder.Configuration.GetSection("ApiService:Ports").Get<int[]>()
           ?? throw new InvalidOperationException("ApiService:Ports is not  configured.");

var cache = builder.AddRedis("credit-order-cache")
    .WithRedisInsight(containerName: "credit-order-insight");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("credid-order-localstack", awsConfig: awsConfig, configureContainer: container =>
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

var cloudFormationTemplate = "CloudFormation/sqs-s3.yml";
var awsResources = builder.AddAWSCloudFormationTemplate("resources", cloudFormationTemplate, "credit-order")
    .WithReference(awsConfig);

var gateway = builder.AddProject<Projects.Api_Gateway>("gateway");
for (var i = 0; i < ports.Length; i++)
{
    var httpsPort = ports[i];
    var httpPort = ports[i] - 1000;

    var generator = builder.AddProject<Projects.Service_Api>($"generator-r{i + 1}", launchProfileName: null)
            .WithReference(cache, "RedisCache")
            .WithHttpEndpoint(httpPort)
            .WithReference(awsResources)
            .WithHttpsEndpoint(httpsPort)
            .WaitFor(cache)
            .WaitFor(awsResources);

    gateway.WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("credit-order-wasm")
    .WaitFor(gateway);

var sink = builder.AddProject<Projects.Service_Storage>("credit-order-sink")
    .WithHttpEndpoint(5444)
    .WithReference(awsResources)
    .WithEnvironment("Settings__MessageBroker", "SQS")
    .WithEnvironment("Settings__S3Hosting", "Minio")
    .WaitFor(awsResources);

var minio = builder.AddMinioContainer("credit-order-minio");

sink.WithEnvironment("AWS__Resources__MinioBucketName", "credit-order-bucket")
    .WithReference(minio)
    .WaitFor(minio);

builder.UseLocalStack(localstack);

builder.Build().Run();
