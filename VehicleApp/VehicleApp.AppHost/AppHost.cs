using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack("vehicle-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;
    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
});

var awsResources = builder
    .AddAWSCloudFormationTemplate("vehicle-resources", "CloudFormation/vehicle-sns.yaml", "vehicle")
    .WithReference(awsConfig);

var minio = builder.AddMinioContainer("vehicle-minio");

var gateway = builder.AddProject<Projects.VehicleApp_Gateway>("vehicleapp-gateway");

for (var i = 0; i < 7; i++)
{
    var service = builder.AddProject<Projects.VehicleApp_Api>($"vehicleapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5000 + i)
        .WithReference(redis)
        .WithReference(awsResources)
        .WaitFor(redis)
        .WaitFor(awsResources);
    gateway.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(gateway);

builder.AddProject<Projects.File_Service>("file-service", launchProfileName: null)
    .WithHttpEndpoint(port: 5280)
    .WithReference(awsResources)
    .WithReference(minio)
    .WithEnvironment("AWS__Resources__MinioBucketName", "vehicle-bucket")
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns")
    .WaitFor(awsResources)
    .WaitFor(minio);

builder.UseLocalStack(localstack);

builder.Build().Run();
