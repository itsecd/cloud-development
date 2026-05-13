using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("employee-api-gateway");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("employee-localstack", awsConfig: awsConfig, configureContainer: container =>
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

var cloudFormationTemplate = "CloudFormation/employee-template-sns-s3.yaml";
var awsResources = builder.AddAWSCloudFormationTemplate("resources", cloudFormationTemplate, "employee")
    .WithReference(awsConfig);

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"employee-api-{i + 1}", launchProfileName: null)
        .WithHttpsEndpoint(7170 + i)
        .WithReference(cache, "RedisCache")
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(cache)
        .WaitFor(awsResources);
    gateway.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("employee-wasm")
    .WaitFor(gateway);

var sink = builder.AddProject<Projects.Event_Sink>("employee-sink")
    .WithReference(awsResources)
    .WithEnvironment("Settings__MessageBroker", "SNS")
    .WithEnvironment("Settings__S3Hosting", "Localstack")
    .WaitFor(awsResources);

sink.WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5134/api/sns");

builder.UseLocalStack(localstack);

builder.Build().Run();
