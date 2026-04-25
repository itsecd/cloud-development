using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "redis-insight");

var gateway = builder.AddProject<Projects.CompanyEmployee_ApiGateway>("api-gateway")
    .WithEndpoint("https", e => e.Port = 7200)
    .WithExternalHttpEndpoints();

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
        container.AdditionalEnvironmentVariables
            .Add("DEBUG", "1");
        container.AdditionalEnvironmentVariables
            .Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
    });

var awsResources = builder
    .AddAWSCloudFormationTemplate("resources", "CloudFormation/company-employee-template-sns.yaml", "company-employee")
    .WithReference(awsConfig);

const int startGeneratorPort = 7301;
for (var i = 0; i < 5; ++i)
{
    var generator = builder.AddProject<Projects.CompanyEmployee_Generator>($"generator-{i}")
        .WithEndpoint("https", e => e.Port = startGeneratorPort + i)
        .WithReference(cache)
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(cache)
        .WaitFor(awsResources);

    gateway.WaitFor(generator);
}

var minio = builder.AddMinioContainer("minio");

var eventSink = builder.AddProject<Projects.CompanyEmployee_EventSink>("event-sink")
    .WithReference(awsResources)
    .WithEnvironment("Settings__MessageBroker", "SNS")
    .WithEnvironment("Settings__S3Hosting", "Minio")
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5225/api/sns")
    .WithEnvironment("AWS__Resources__MinioBucketName", "company-employee-bucket")
    .WithReference(minio)
    .WaitFor(minio)
    .WaitFor(awsResources);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();