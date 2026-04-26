using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("employee-localstack", awsConfig: awsConfig, configureContainer: container =>
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

var awsResources = builder.AddAWSCloudFormationTemplate("resources", "CloudFormation/employee-template.yaml", "employee")
    .WithReference(awsConfig);

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.EmployeeApp_Api>($"employee-app-{i}", launchProfileName: null)
        .WithReference(cache)
        .WithReference(awsResources)
        .WaitFor(cache)
        .WaitFor(awsResources)
        .WithHttpsEndpoint(port: 5100 + i, name: $"employee-https-{i}");
    apiGateway.WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.AddProject<Projects.File_Service>("file-service")
    .WithReference(awsResources)
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns")
    .WaitFor(awsResources);

builder.UseLocalStack(localstack);

builder.Build().Run();
