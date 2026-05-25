using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using ProjectApp.FileService;
using ProjectApp.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var accessKey = builder.Configuration["Aws:AccessKey"] ?? "test";
var secretKey = builder.Configuration["Aws:SecretKey"] ?? "test";
var region = builder.Configuration["Aws:Region"] ?? "us-east-1";

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(
    new BasicAWSCredentials(accessKey, secretKey),
    new AmazonSQSConfig
    {
        ServiceURL = builder.Configuration["Sqs:ServiceUrl"] ?? "http://localhost:4566",
        AuthenticationRegion = region
    }));

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    new BasicAWSCredentials(
        builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
        builder.Configuration["Minio:SecretKey"] ?? "minioadmin"),
    new AmazonS3Config
    {
        ServiceURL = builder.Configuration["Minio:ServiceUrl"] ?? "http://localhost:9000",
        AuthenticationRegion = region,
        ForcePathStyle = true
    }));

builder.Services.AddHostedService<CreditApplicationFilePersistenceWorker>();

var host = builder.Build();
host.Run();
