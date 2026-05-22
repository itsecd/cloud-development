using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("patient-cache")
    .WithRedisInsight(containerName: "patient-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("patient-localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    });

var awsResources = builder.AddAWSCloudFormationTemplate("resources", "CloudFormation/patient-template-sqs.yaml", "patient")
    .WithReference(awsConfig);

for (var i = 0; i < 5; i++)
{
    var generator = builder.AddProject<Projects.Patient_Generator>($"generator-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5200 + i)
        .WithReference(cache, "patient-cache")
        .WithReference(awsResources)
        .WaitFor(cache)
        .WaitFor(awsResources);
    gateway.WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

var minio = builder.AddMinioContainer("patient-minio");

builder.AddProject<Projects.File_Service>("file-service")
    .WithReference(awsResources)
    .WithReference(minio)
    .WithEnvironment("AWS__Resources__MinioBucketName", "patient-bucket")
    .WaitFor(awsResources)
    .WaitFor(minio);

builder.UseLocalStack(localstack);

builder.Build().Run();
