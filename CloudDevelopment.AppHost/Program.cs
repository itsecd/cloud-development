using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

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
    .AddAWSCloudFormationTemplate("employee-contracts-resources", "CloudFormation/employee-contracts-template.yaml", "employee-contracts")
    .WithReference(awsConfig);

var gateway = builder.AddProject<Projects.Api_Gateway>("api-gateway")
    .WithHttpsEndpoint(port: 7129)
    .WithHttpEndpoint(port: 5163);

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.ContractGenerator_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 25000 + i)
        .WithReference(redis)
        .WithReference(awsResources)
        .WaitFor(redis)
        .WaitFor(awsResources);
    gateway.WaitFor(service);
}

builder.AddProject<Projects.ContractGenerator_FileService>("file-service")
    .WithReference(awsResources)
    .WaitFor(awsResources);

builder.AddProject<Projects.Client_Wasm>("client")
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();
