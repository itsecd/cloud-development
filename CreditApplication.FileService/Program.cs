using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using CreditApplication.FileService.Services;
using CreditApplication.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var awsServiceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var credentials = new BasicAWSCredentials("test", "test");

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(credentials, new AmazonS3Config
{
    ServiceURL = awsServiceUrl,
    ForcePathStyle = true
}));

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(credentials, new AmazonSQSConfig
{
    ServiceURL = awsServiceUrl
}));

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
    new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = awsServiceUrl
    }));

builder.Services.AddSingleton<S3StorageService>();
builder.Services.AddHostedService<AwsResourceInitializer>();
builder.Services.AddHostedService<SqsListenerService>();

builder.Services.AddHealthChecks()
    .AddCheck<LocalStackHealthCheck>("sqs")
    .AddCheck<S3HealthCheck>("s3");

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

app.MapGet("/files", async (S3StorageService storage) =>
    Results.Ok(await storage.ListFilesAsync()));

app.MapGet("/files/{key}", async (string key, S3StorageService storage) =>
{
    var content = await storage.GetFileAsync(key);
    return content is not null ? Results.Text(content, "application/json") : Results.NotFound();
});

app.Run();
