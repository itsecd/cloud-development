using Amazon.SimpleNotificationService;
using CourseManagement.ApiService.Cache;
using CourseManagement.ApiService.Entities;
using CourseManagement.ApiService.Generator;
using CourseManagement.ApiService.Messaging;
using CourseManagement.ApiService.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Redis
builder.AddRedisDistributedCache("course-cache");

// Add services to the container
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Регистрация LocalStack
builder.Services.AddLocalStack(builder.Configuration);

// Регистрация AWS сервисов
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

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