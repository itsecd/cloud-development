using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using Service.Storage.Broker;
using Service.Storage.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddScoped<SnsSubscriptionService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.AddMinioClient("programproj-minio");
builder.Services.AddScoped<IS3Service, S3MinioService>();

var app = builder.Build();
await app.Services.CreateScope().ServiceProvider.GetRequiredService<SnsSubscriptionService>().SubscribeEndpoint();
await app.Services.CreateScope().ServiceProvider.GetRequiredService<IS3Service>().EnsureBucketExists();

app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
