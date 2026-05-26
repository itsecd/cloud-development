using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var awsUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var credentials = new BasicAWSCredentials("test", "test");

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(credentials, new AmazonS3Config
{
    ServiceURL = awsUrl,
    ForcePathStyle = true
}));

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(credentials, new AmazonSQSConfig { ServiceURL = awsUrl }));
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
    new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig { ServiceURL = awsUrl }));

builder.Services.AddSingleton<S3StorageService>();
builder.Services.AddHostedService<AwsResourceInitializer>();  
builder.Services.AddHostedService<SqsListenerService>();
var app = builder.Build();

app.MapGet("/files", async (S3StorageService storage) =>
    Results.Ok(await storage.ListFilesAsync()));

app.MapGet("/files/{key}", async (string key, S3StorageService storage) =>
{
    var content = await storage.GetFileAsync(key);
    return content != null
        ? Results.Text(content, "application/json")
        : Results.NotFound();
});

app.MapDefaultEndpoints();
app.Run();