using Amazon.S3;
using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using CourseManagement.Storage.Messaging;
using CourseManagement.Storage.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка LocalStack
builder.Services.AddLocalStack(builder.Configuration);

// Получение конфигурации 
var configuration = builder.Configuration;
var region = configuration["AWS:Region"] ?? throw new KeyNotFoundException("AWS region was not found in configuration");
var accessKey = configuration["AWS:AccessKeyId"] ?? throw new KeyNotFoundException("AWS access key ID link was not found in configuration");
var secretKey = configuration["AWS:SecretAccessKey"] ?? throw new KeyNotFoundException("AWS secret access key was not found in configuration");
var s3Url = configuration["S3:ServiceURL"] ?? throw new KeyNotFoundException("S3 service URL was not found in configuration");
var snsUrl = configuration["SNS:ServiceURL"] ?? throw new KeyNotFoundException("SNS service URL was not found in configuration");

// Регистрация AWS сервисов
builder.Services.AddSingleton<IAmazonS3>(
    new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
    {
        ServiceURL = s3Url,
        UseHttp = true,
        AuthenticationRegion = region
    })
);
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(
    new AmazonSimpleNotificationServiceClient(accessKey, secretKey, new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = snsUrl,
        UseHttp = true,
        AuthenticationRegion = region
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

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// App
app.MapControllers();
app.Run();