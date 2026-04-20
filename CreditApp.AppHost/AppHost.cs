using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander(containerName: "redis-commander");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.USEast1);

var localstack = builder
    .AddLocalStack("localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    });

var awsResources = builder.AddAWSCloudFormationTemplate("resources", "CloudFormation/credit-template.yaml", "credit")
    .WithReference(awsConfig);

var gateway = builder.AddProject<Projects.CreditApp_Gateway>("gateway")
    .WithEndpoint("https", e =>
    {
        e.Port = 9002;
        e.IsProxied = false;
        e.UriScheme = "https";
    })
    .WithExternalHttpEndpoints();

for (var i = 0; i < 5; i++)
{
    var port = 7401 + i;
    var api = builder.AddProject<Projects.CreditApp_Api>($"api{i + 1}")
        .WithEndpoint("https", e =>
        {
            e.Port = port;
            e.IsProxied = false;
            e.UriScheme = "https";
        })
        .WithReference(redis)
        .WithReference(awsResources)
        .WaitFor(redis)
        .WaitFor(awsResources);

    gateway.WaitFor(api);
}

builder.AddProject<Projects.CreditApp_FileService>("fileservice")
    .WithReference(awsResources)
    .WaitFor(awsResources);

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

builder.UseLocalStack(localstack);

builder.Build().Run();
