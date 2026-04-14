using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using CourseManagement.Storage.Messaging;
using CourseManagement.Storage.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка LocalStack
builder.Services.AddLocalStack(builder.Configuration);

// Регистрация AWS сервисов
var configuration = builder.Configuration;

var s3Url = configuration["S3:ServiceURL"] ?? throw new KeyNotFoundException("S3 service url was not found in configuration");
var s3Region = configuration["S3:Region"] ?? throw new KeyNotFoundException("S3 region was not found in configuration");
var s3AccessKey = configuration["S3:AccessKeyId"] ?? throw new KeyNotFoundException("S3 access key id link was not found in configuration");
var s3SecretKey = configuration["S3:SecretAccessKey"] ?? throw new KeyNotFoundException("S3 secret access key was not found in configuration");

var snsUrl = configuration["SNS:ServiceURL"] ?? throw new KeyNotFoundException("SNS service url was not found in configuration");
var snsRegion = configuration["SNS:Region"] ?? throw new KeyNotFoundException("SNS region was not found in configuration");
var snsAccessKey = configuration["SNS:AccessKeyId"] ?? throw new KeyNotFoundException("SNS access key id was not found in configuration");
var snsSecretKey = configuration["SNS:SecretAccessKey"] ?? throw new KeyNotFoundException("SNS secret access key was not found in configuration");

builder.Services.AddSingleton<IAmazonS3>(
    new AmazonS3Client(s3AccessKey, s3SecretKey, new AmazonS3Config
    {
        ServiceURL = s3Url,
        UseHttp = true,
        AuthenticationRegion = s3Region
    })
);

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(
    new AmazonSimpleNotificationServiceClient(snsAccessKey, snsSecretKey, new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = snsUrl,
        UseHttp = true,
        AuthenticationRegion = snsRegion
    })
);

// Регистрация SNS и S3 сервисов
builder.Services.AddSingleton<IS3Service, S3AwsService>();
builder.Services.AddSingleton<ISubscriberService, SnsSubscriberService>();

var app = builder.Build();

// Инициализация S3 bucket при старте
using (var scope = app.Services.CreateScope())
{
    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3Service.EnsureBucketExists();

    var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriberService>();
    await subscriptionService.SubscribeEndpoint();
}

// Настройка pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();