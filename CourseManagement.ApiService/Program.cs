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

// Регистрация AWS сервисов
var configuration = builder.Configuration;

var snsUrl = configuration["SNS:ServiceURL"] ?? throw new KeyNotFoundException("SNS service url was not found in configuration");
var snsRegion = configuration["SNS:Region"] ?? throw new KeyNotFoundException("SNS region was not found in configuration");
var snsAccessKey = configuration["SNS:AccessKeyId"] ?? throw new KeyNotFoundException("SNS access key id was not found in configuration");
var snsSecretKey = configuration["SNS:SecretAccessKey"] ?? throw new KeyNotFoundException("SNS secret access key was not found in configuration");

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(
    new AmazonSimpleNotificationServiceClient(snsAccessKey, snsSecretKey, new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = snsUrl,
        UseHttp = true,
        AuthenticationRegion = snsRegion
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
builder.Services.AddSingleton<IPublisherService<Course>, SnsPublisherService<Course>>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();

// Mapping
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();