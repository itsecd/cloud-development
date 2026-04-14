using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("residential-building-cache")
    .WithRedisInsight(containerName: "residential-building-insight");

var gateway = builder.AddProject<Projects.ResidentialBuilding_Gateway>("gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 5300)
    .WithExternalHttpEndpoints();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder
    .AddLocalStack("residential-building-localstack", awsConfig: awsConfig, configureContainer: container =>
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
    .AddAWSCloudFormationTemplate("resources", "CloudFormation/residential-building-template-sns-s3.yaml", "residential-building")
    .WithReference(awsConfig);

const int generatorPortBase = 5200;
for (var i = 1; i <= 5; ++i)
{
    var generator = builder.AddProject<Projects.ResidentialBuilding_Generator>($"generator-{i}")
        .WithReference(cache, "residential-building-cache")
        .WithEndpoint("http", endpoint => endpoint.Port = generatorPortBase + i)
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(cache)
        .WaitFor(awsResources);

    gateway.WaitFor(generator);
}

builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();