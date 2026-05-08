using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

var gateway = builder.AddProject<Projects.API_Gateway>("api-gateway");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    });

var awsResources = builder
    .AddAWSCloudFormationTemplate("resources", "CloudFormation/employee-queue-template.yaml", "cloud-employee")
    .WithReference(awsConfig);

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.Cloud_API>($"api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 8000 + i)
        .WithReference(redis)
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SQS")
        .WaitFor(redis)
        .WaitFor(awsResources);
    gateway.WaitFor(api);
}

var minio = builder.AddMinioContainer("minio");

var eventSink = builder.AddProject<Projects.Cloud_EventSink>("event-sink")
    .WithReference(awsResources)
    .WithReference(minio)
    .WithEnvironment("Settings__MessageBroker", "SQS")
    .WithEnvironment("Settings__S3Hosting", "Minio")
    .WithEnvironment("AWS__Resources__MinioBucketName", "cloud-employee-bucket")
    .WaitFor(minio)
    .WaitFor(awsResources);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();
