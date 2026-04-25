using Amazon.SimpleNotificationService;
using File.Service.Messaging;
using File.Service.Storage;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.Services.AddScoped<SnsSubscriptionService>();

builder.AddMinioClient("vehicle-minio");
builder.Services.AddScoped<IS3Service, MinioS3Service>();

var app = builder.Build();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var s3 = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3.EnsureBucketExists();

    var subscription = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
    await subscription.SubscribeEndpoint();
}

app.MapControllers();

app.Run();
