using Amazon;
using Microsoft.Extensions.Configuration;
using Aspire.Hosting.LocalStack.Container;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("programproj-cache").WithRedisInsight(containerName: "programproj-insight");

var ports = builder.Configuration.GetSection("ApiGateway:Ports").Get<int[]>()
    ?? throw new InvalidOperationException("Configuration section 'ApiGateway:Ports' is missing or empty in appsettings.json file.");

var apiGW = builder.AddProject<Projects.Service_Gateway>("api-gw", project =>
{
    project.ExcludeLaunchProfile = true;
})
.WithHttpEndpoint(port: 5247)
.WithHttpsEndpoint(port: 7198);

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack("programproj-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;
    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
});

var templatePath = Path.Combine(
    builder.AppHostDirectory,
    "Cloudformation",
    "programproj-template-sns.yml");

var awsResources = builder.AddAWSCloudFormationTemplate(
        "resources",
        templatePath,
        "programproj")
    .WithReference(awsConfig);

var storage = builder.AddProject<Projects.Service_Storage>("programproj-storage", project =>
{
    project.ExcludeLaunchProfile = true;
})
.WithReference(awsResources)
.WithHttpEndpoint(port: 5280)
.WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns")
.WaitFor(awsResources);

var minio = builder.AddMinioContainer("programproj-minio");

storage.WithEnvironment("AWS__Resources__MinioBucketName", "programproj-bucket")
    .WithReference(minio)
    .WaitFor(minio);

for (var i = 0; i < ports.Length; i++)
{
    var httpsPort = ports[i];
    var httpPort = httpsPort - 1000;

    var service = builder.AddProject<Projects.Service_Api>($"programproj-api{i + 1}", project =>
    {
        project.ExcludeLaunchProfile = true;
    })
    .WithReference(cache, "RedisCache")
    .WithHttpEndpoint(port: httpPort)
    .WithHttpsEndpoint(port: httpsPort)
    .WithReference(awsResources)
    .WithHttpHealthCheck("/health", endpointName: "https")
    .WaitFor(cache)
    .WaitFor(awsResources);

    apiGW.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("programproj-wasm")
    .WaitFor(apiGW);

builder.UseLocalStack(localstack);

builder.Build().Run();