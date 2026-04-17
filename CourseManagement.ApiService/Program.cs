using Amazon.SimpleNotificationService;
using CourseManagement.ApiService.Cache;
using CourseManagement.ApiService.Entities;
using CourseManagement.ApiService.Generator;
using CourseManagement.ApiService.Messaging;
using CourseManagement.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Redis
builder.AddRedisDistributedCache("course-cache");

// Add services to the container
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Получение конфигурации 
var configuration = builder.Configuration;
var snsUrl = configuration["SNS:ServiceURL"] ?? throw new KeyNotFoundException("SNS service URL was not found in configuration");
var region = configuration["AWS:Region"] ?? throw new KeyNotFoundException("AWS region was not found in configuration");
var accessKey = configuration["AWS:AccessKeyId"] ?? throw new KeyNotFoundException("AWS access key ID was not found in configuration");
var secretKey = configuration["AWS:SecretAccessKey"] ?? throw new KeyNotFoundException("AWS secret access key was not found in configuration");

// Регистрация AWS сервисов
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(
    new AmazonSimpleNotificationServiceClient(accessKey, secretKey, new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = snsUrl,
        UseHttp = true,
        AuthenticationRegion = region
    })
);

// Контроллеры
builder.Services.AddControllers();

// Генератор курсов
builder.Services.AddSingleton<ICourseGenerator, CourseGenerator> ();

// Сервис для взаимодействия с кэшем
builder.Services.AddSingleton<ICacheService<Course>, CacheService<Course>>();

// Сервис для сущности типа Курс
builder.Services.AddSingleton<ICourseService, CourseService>();

// Сервис для публикации данных
builder.Services.AddSingleton<IPublisherService, SnsPublisherService>();

var app = builder.Build();

app.UseExceptionHandler();

// Mapping
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();