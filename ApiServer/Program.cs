using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using ApiServer;
using Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "employees:";
});

var sqsServiceUrl = builder.Configuration["Sqs:ServiceUrl"] ?? "http://localhost:4566";

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var config = new AmazonSQSConfig
    {
        ServiceURL = sqsServiceUrl,
        AuthenticationRegion = "us-east-1"
    };
    var credentials = new BasicAWSCredentials("test", "test");
    return new AmazonSQSClient(credentials, config);
});

builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<EmployeeStore>();
builder.Services.AddHostedService<SqsConsumerService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

app.MapGet("/", () => "ApiServer is running");

app.MapGet("/api/employees", async (EmployeeStore store) =>
{
    var employees = await store.GetAllAsync();
    return Results.Ok(employees);
});

app.MapGet("/api/employees/{id:int}", async (int id, EmployeeStore store) =>
{
    var employee = await store.GetByIdAsync(id);
    return employee is not null ? Results.Ok(employee) : Results.NotFound();
});

app.MapPost("/api/employees/generate/{count:int}", async (int count, EmployeeStore store) =>
{
    if (count <= 0 || count > 1000)
        return Results.BadRequest("Count must be between 1 and 1000");

    var employees = EmployeeGenerator.Generate(count);
    await store.AddEmployeesAsync(employees);
    return Results.Ok(new { message = "Generated " + count + " employees", employees });
});

app.Run();
