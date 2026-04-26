using Amazon.Runtime;
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

// LocalStack config
builder.Services.AddLocalStack(builder.Configuration);

// SNS client
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
{
    var serviceUrl = builder.Configuration["AWS:ServiceURL"]
        ?? "http://localhost:4566";

    var region = builder.Configuration["AWS:Region"]
        ?? builder.Configuration["AWS_REGION"]
        ?? builder.Configuration["AWS_DEFAULT_REGION"]
        ?? "eu-central-1";

    var config = new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = serviceUrl,
        AuthenticationRegion = region
    };

    return new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials("test", "test"),config);
});

// Controllers
builder.Services.AddControllers();

// DI
builder.Services.AddSingleton<Generator>();
builder.Services.AddSingleton<IInventoryCache, InventoryCache>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IProducerService, SnsPublisherService>();

var app = builder.Build();

app.Logger.LogInformation("SNS ServiceURL: {ServiceURL}", builder.Configuration["AWS:ServiceURL"]);
app.Logger.LogInformation("SNS Region: {Region}", builder.Configuration["AWS:Region"]);
app.Logger.LogInformation("SNS TopicArn: {TopicArn}", builder.Configuration["AWS:Resources:SNSTopicArn"]);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();