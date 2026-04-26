using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("RedisCache").WithRedisInsight(containerName: "insight");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack("warehouse-localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Session;
    container.DebugLevel = 1;
    container.LogLevel = LocalStackLogLevel.Debug;
    container.Port = 4566;
    container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
});

var awsResources = builder.AddAWSCloudFormationTemplate("warehouse-resources", "CloudFormation/warehouse-template.yaml", "warehouse")
    .WithReference(awsConfig);

var ports = new[] { 5001, 5002, 5003 };

var gateway = builder.AddProject<Projects.AspireApp_ApiGateway>("api-gateway")
    .WithHttpEndpoint(port: 5101, name: "gateway");

for (var i = 0; i < 3; i++)
{
    var api = builder.AddProject<Projects.AspireApp_ApiService>($"warehouse-api-{i}")
        .WithReference(cache)
        .WithReference(awsResources)
        .WithEnvironment("REPLICA_ID", i.ToString())
        .WithEnvironment("Settings__MessageBroker", "SQS")
        .WithHttpEndpoint(port: ports[i], name: $"api-{i}")
        .WaitFor(cache)
        .WaitFor(awsResources);

    gateway = gateway.WaitFor(api);
}

builder.AddProject<Projects.AspireApp_FileService>("warehouse-fileservice")
    .WithReference(awsResources)
    .WithEnvironment("Settings__MessageBroker", "SQS")
    .WithEnvironment("Settings__S3Hosting", "Localstack")
    .WaitFor(awsResources);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(gateway)
    .WithHttpEndpoint(port: 5127, name: "client")
    .WaitFor(gateway);

builder.UseLocalStack(localstack);

builder.Build().Run();
