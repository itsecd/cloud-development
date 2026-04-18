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

// Регистрация AWS сервисов
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

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