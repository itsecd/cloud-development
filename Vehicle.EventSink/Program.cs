using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Minio;
using Vehicle.EventSink.Messaging;
using Vehicle.EventSink.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(CreateSnsClient);
builder.Services.AddSingleton<IMinioClient>(CreateMinioClient);

builder.Services.AddSingleton<IS3Service, S3MinioService>();
builder.Services.AddSingleton<SnsSubscriptionService>();

var app = builder.Build();

LogConfiguration(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "Vehicle.EventSink",
    status = "ok",
    message = "Vehicle EventSink is running"
}));

app.MapControllers();
app.MapDefaultEndpoints();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        await InitializeEventSinkAsync(app);
    });
});

await app.RunAsync();

static IAmazonSimpleNotificationService CreateSnsClient(IServiceProvider serviceProvider)
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    return new AmazonSimpleNotificationServiceClient(
        new BasicAWSCredentials("test", "test"),
        new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = Required(configuration, "AWS:ServiceUrl"),
            AuthenticationRegion = Required(configuration, "AWS:Region")
        });
}

static IMinioClient CreateMinioClient(IServiceProvider serviceProvider)
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    var endpoint = Required(configuration, "AWS:Resources:MinioEndpoint")
        .Replace("http://", string.Empty)
        .Replace("https://", string.Empty);

    return new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(
            Required(configuration, "AWS:Resources:MinioAccessKey"),
            Required(configuration, "AWS:Resources:MinioSecretKey"))
        .WithSSL(false)
        .Build();
}

static async Task InitializeEventSinkAsync(WebApplication app)
{
    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Vehicle.EventSink.Startup");

    const int maxAttempts = 5;
    var retryDelay = TimeSpan.FromSeconds(3);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            logger.LogInformation(
                "Initializing Vehicle.EventSink. Attempt {Attempt}/{MaxAttempts}",
                attempt,
                maxAttempts);

            using var scope = app.Services.CreateScope();

            var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
            await s3Service.EnsureBucketExists();

            var subscriptionService = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
            await subscriptionService.SubscribeEndpoint();

            logger.LogInformation("Vehicle.EventSink was initialized successfully");
            return;
        }
        catch (Exception ex)
        {
            if (attempt == maxAttempts)
            {
                logger.LogError(
                    ex,
                    "Vehicle.EventSink initialization failed after all attempts");
            }
            else
            {
                logger.LogWarning(
                    ex,
                    "Vehicle.EventSink is not ready yet. Retry attempt {Attempt}/{MaxAttempts}",
                    attempt,
                    maxAttempts);

                await Task.Delay(retryDelay);
            }
        }
    }
}

static string Required(IConfiguration configuration, string key)
{
    return configuration[key]
        ?? throw new KeyNotFoundException($"{key} was not found in configuration");
}

static void LogConfiguration(WebApplication app)
{
    app.Logger.LogInformation("AWS Region: {Region}", app.Configuration["AWS:Region"]);
    app.Logger.LogInformation("SNS Topic ARN: {TopicArn}", app.Configuration["AWS:Resources:SNSTopicArn"]);
    app.Logger.LogInformation("SNS endpoint URL: {SnsUrl}", app.Configuration["AWS:Resources:SNSUrl"]);
    app.Logger.LogInformation("Minio endpoint: {MinioEndpoint}", app.Configuration["AWS:Resources:MinioEndpoint"]);
    app.Logger.LogInformation("Minio bucket: {Bucket}", app.Configuration["AWS:Resources:MinioBucketName"]);
}