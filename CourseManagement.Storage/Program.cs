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
var useLocalStack = builder.Configuration.GetValue<bool>("LocalStack:UseLocalStack");
if (useLocalStack)
{
    builder.Services.AddLocalStack(builder.Configuration);
}

// Регистрация AWS сервисов
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

// Регистрация SNS и S3 сервисов
builder.Services.AddScoped<IS3Service, S3AwsService>();
builder.Services.AddScoped<SnsSubscriptionService>();

var app = builder.Build();

// Инициализация S3 bucket при старте
using (var scope = app.Services.CreateScope())
{
    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3Service.EnsureBucketExists();

    var subscriptionService = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
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