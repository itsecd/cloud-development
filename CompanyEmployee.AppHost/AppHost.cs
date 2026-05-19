using Amazon;

Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis").WithRedisInsight();

var api = builder.AddProject<Projects.CompanyEmployee_ApiGateway>("companyemployee-apigateway")
    .WithHttpEndpoint(name: "gateway", port: 5212);

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack(
    "companyemployee-localstack",
    awsConfig: awsConfig,
    configureContainer: container =>
    {
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
    });


builder.UseLocalStack(localstack);

var minio = builder.AddMinioContainer("minio")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WaitFor(redis);

minio.WithEndpoint("http", endpoint => endpoint.Port = 9000);
minio.WithEndpoint("console", endpoint => endpoint.Port = 9001);

for (var i = 1; i <= 3; i++)
{
    var generator = builder
        .AddProject<Projects.CompanyEmployee_ApiService>($"generator-{i}")
        .WithReference(redis)
        .WithReference(awsConfig)
        .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(redis)
        .WithHttpEndpoint(name: $"http");

    api.WithReference(generator)
        .WaitFor(generator);
}

builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5280;
    })
    .WithReference(redis)
    .WithReference(awsConfig)
    .WithReference(minio)
    .WaitFor(minio)
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns")
    .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
    .WithEnvironment("Settings__MessageBroker", "SNS")
    .WithEnvironment("Minio__Endpoint", "localhost:9000")
    .WithEnvironment("Minio__AccessKey", "minioadmin")
    .WithEnvironment("Minio__SecretKey", "minioadmin")
    .WithEnvironment("Minio__BucketName", "companyemployee");


builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();