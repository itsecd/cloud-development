using Amazon.S3;
using Amazon.SimpleNotificationService;
using Inventory.FileService.Messaging;
using Inventory.FileService.Storage;
using Inventory.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

builder.Services.AddScoped<IS3Service, S3AwsService>();
builder.Services.AddScoped<ISubscriberService, SnsSubscriberService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3Service.EnsureBucketExists();

    var brokerType = app.Configuration["Settings:MessageBroker"]
        ?? app.Configuration["Settings__MessageBroker"];

    if (string.Equals(brokerType, "SNS", StringComparison.OrdinalIgnoreCase))
    {
        var subscriberService = scope.ServiceProvider.GetRequiredService<ISubscriberService>();
        await subscriberService.SubscribeEndpoint();
    }
}

await app.RunAsync();