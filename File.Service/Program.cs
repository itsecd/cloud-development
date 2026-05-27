using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using File.Service.Messaging;
using File.Service.Storage;

var localstackHost = Environment.GetEnvironmentVariable("LOCALSTACK_HOST") ?? "localhost";
var localstackPort = Environment.GetEnvironmentVariable("LOCALSTACK_PORT") ?? "14566";
var serviceUrl = $"http://{localstackHost}:{localstackPort}";

var credentials = new BasicAWSCredentials("test", "test");

var s3Config = new AmazonS3Config
{
    ServiceURL = serviceUrl,
    UseHttp = true,
    AuthenticationRegion = "eu-central-1",
    ForcePathStyle = true
};

var sqsConfig = new AmazonSQSConfig
{
    ServiceURL = serviceUrl,
    UseHttp = true,
    AuthenticationRegion = "eu-central-1"
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));
builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, sqsConfig));

builder.Services.AddScoped<IVehicleStorageService, S3VehicleStorageService>();
builder.Services.AddHostedService<SqsVehicleConsumerService>();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
    var sqsClient = scope.ServiceProvider.GetRequiredService<IAmazonSQS>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var putRequest = new PutBucketRequest
        {
            BucketName = "vehicle-storage-bucket",
            BucketRegion = "eu-central-1"
        };
        await s3Client.PutBucketAsync(putRequest);
        logger.LogInformation("Bucket created successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create bucket");
    }

    try
    {
        await sqsClient.CreateQueueAsync("vehicle-queue");
        logger.LogInformation("Queue created successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create queue");
    }
}

app.MapControllers();
app.Run();