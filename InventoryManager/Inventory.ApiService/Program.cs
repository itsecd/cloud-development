using Amazon.SimpleNotificationService;
using Inventory.ApiService.Cache;
using Inventory.ApiService.Generation;
using Inventory.ApiService.Messaging;
using Inventory.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Cache
builder.AddRedisDistributedCache("cache");

// AWS SNS
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddSingleton<Generator>();
builder.Services.AddScoped<IInventoryCache, InventoryCache>();
builder.Services.AddScoped<IProducerService, SnsPublisherService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();