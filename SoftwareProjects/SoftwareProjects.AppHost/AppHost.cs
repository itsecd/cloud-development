using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight(containerName: "softwareprojects-redis-insight");

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("softwareprojects-localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
        container.AdditionalEnvironmentVariables
            .Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
    });

var awsResources = builder
    .AddAWSCloudFormationTemplate("resources", "CloudFormation/softwareprojects-template.yaml", "softwareprojects")
    .WithReference(awsConfig);

var fileService = builder.AddProject<Projects.File_Service>("file-service")
    .WithReference(awsResources)
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5300/api/sns")
    .WaitFor(awsResources);

for (var i = 0; i < 5; i++)
{
    var softwareProjectsApi = builder.AddProject<Projects.SoftwareProjects_Api>($"softwareprojects-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5200 + i)
        .WithReference(cache)
        .WithReference(awsResources)
        .WaitFor(cache)
        .WaitFor(awsResources);
    gateway.WaitFor(softwareProjectsApi);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();
