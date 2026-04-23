using Amazon.SimpleNotificationService;
using Inventory.ApiService.Cache;
using Inventory.ApiService.Generation;
using Inventory.ApiService.Messaging;
using Inventory.ApiService.Services;
using Inventory.ServiceDefaults;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Redis
builder.AddRedisDistributedCache("cache");

// OpenAPI / errors
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// LocalStack
builder.Services.AddLocalStack(builder.Configuration);

// AWS SNS
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

// Controllers
builder.Services.AddControllers();

// DI
builder.Services.AddSingleton<Generator>();
builder.Services.AddSingleton<IInventoryCache, InventoryCache>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IProducerService, SnsPublisherService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();