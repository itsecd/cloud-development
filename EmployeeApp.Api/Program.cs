using Amazon.SimpleNotificationService;
using EmployeeApp.Api.Messaging;
using EmployeeApp.Api.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.Services.AddScoped<IProducerService, SnsPublisherService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/employees", async (IEmployeeService service, int id) =>
    await service.GetEmployeeById(id));

app.Run();
