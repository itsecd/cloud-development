using Amazon;
using Aspire.Hosting.LocalStack;
using Aspire.Hosting.LocalStack.Container;
using LocalStack.Client.Enums;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var minio = builder.AddMinioContainer("minio")
    .WithDataVolume("minio-data");

var awsConfig = builder.AddAWSSDKConfig()
    .WithRegion(RegionEndpoint.USEast1);

var localstack = builder.AddLocalStack("localstack", awsConfig: awsConfig, configureContainer: container =>
{
    container.Lifetime = ContainerLifetime.Persistent;
    container.EagerLoadedServices = [AwsService.Sns];
    container.LogLevel = LocalStackLogLevel.Warn;
    container.Port = 4566;
});
builder.UseLocalStack(localstack);

var fileService = builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WithReference(minio)
    .WithReference(localstack)
    .WithEnvironment("MinIO__BucketName", "employee-data")
    .WaitFor(minio);

var gateway = builder.AddProject<Projects.CompanyEmployee_Gateway>("gateway")
    .WithExternalHttpEndpoints();

const int startApiHttpsPort = 6001;
var apiReplicas = new List<IResourceBuilder<ProjectResource>>();

for (var i = 0; i < 5; i++)
{
    var httpsPort = startApiHttpsPort + i;

    var api = builder.AddProject<Projects.CompanyEmployee_Api>($"api-{i + 1}")
        .WithReference(redis)
        .WithReference(localstack)
        .WithReference(awsConfig)
        .WithEndpoint("https", e => e.Port = httpsPort)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WaitFor(redis);

    apiReplicas.Add(api);
    gateway.WaitFor(api);
}

foreach (var replica in apiReplicas)
{
    gateway.WithReference(replica);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();