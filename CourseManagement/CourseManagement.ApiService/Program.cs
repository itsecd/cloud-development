using CourseManagement.ApiService.Dto;
using CourseManagement.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// Redis
builder.AddRedisDistributedCache("course-cache");

// Add services to the container
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Контроллеры
builder.Services.AddControllers();

// Генератор курсов
builder.Services.AddSingleton<CourseGenerator>();

// Сервис для взаимодействия с кэшем
builder.Services.AddSingleton<CacheService<CourseDto>>();

// Сервис для сущности типа Курс
builder.Services.AddSingleton<CourseService>();

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