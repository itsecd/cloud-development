using Amazon.S3;
using Amazon.SimpleNotificationService;
using File.Service.Messaging;
using File.Service.Storage;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.Services.AddScoped<IS3Service, S3AwsService>();
builder.Services.AddScoped<SnsSubscriptionService>();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
await s3Service.EnsureBucketExists();

var subscriptionService = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
await subscriptionService.SubscribeEndpoint();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
