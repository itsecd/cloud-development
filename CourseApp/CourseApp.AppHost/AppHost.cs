using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack("localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;
    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
});

var awsResources = builder
    .AddAWSCloudFormationTemplate("resources", "CloudFormation/courseapp-template.yaml", "courseapp")
    .WithReference(awsConfig);

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var courseApi = builder.AddProject<Projects.CourseApp_Api>($"courseapp-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 5213 + i)
        .WithReference(redis)
        .WithReference(awsResources)
        .WaitFor(redis)
        .WaitFor(awsResources);

    apiGateway.WaitFor(courseApi);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.AddProject<Projects.CourseApp_FileService>("courseapp-fileservice")
    .WithReference(awsResources)
    .WaitFor(awsResources);

builder.UseLocalStack(localstack);

builder.Build().Run();
