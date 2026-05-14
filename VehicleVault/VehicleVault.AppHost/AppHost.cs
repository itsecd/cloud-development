using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack("vehiclevault-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;
    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
});

var awsResources = builder
    .AddAWSCloudFormationTemplate("vehiclevault-resources", "CloudFormation/vehiclevault-template.yaml", "vehiclevault")
    .WithReference(awsConfig);

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var api = builder.AddProject<Projects.VehicleVault_Api>($"vehiclevault-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(8000 + i)
        .WithReference(cache)
        .WithReference(awsResources)
        .WaitFor(cache)
        .WaitFor(awsResources);
    apiGateway.WaitFor(api);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.AddProject<Projects.File_Service>("file-service")
    .WithReference(awsResources)
    .WaitFor(awsResources);

builder.UseLocalStack(localstack);

builder.Build().Run();
