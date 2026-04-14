using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using Service.FileStorage.Messaging;
using Service.FileStorage.Storage;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddScoped<SnsSubscriptionService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

builder.AddMinioClient("creditapp-minio");
builder.Services.AddScoped<IS3Service, S3MinioService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var subscriptionService = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
    await subscriptionService.SubscribeEndpoint();
}

using (var scope = app.Services.CreateScope())
{
    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3Service.EnsureBucketExists();
}

app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
