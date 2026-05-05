using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using Service.Storage.Broker;
using Service.Storage.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddScoped<SnsSubscriptionService>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

builder.AddMinioClient("programproj-minio");
builder.Services.AddScoped<IS3Service, S3MinioService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Storage API v1");
    options.RoutePrefix = "swagger";
});

await using var scope = app.Services.CreateAsyncScope();

var snsSubscriptionService = scope.ServiceProvider
    .GetRequiredService<SnsSubscriptionService>();

var s3Service = scope.ServiceProvider
    .GetRequiredService<IS3Service>();

await snsSubscriptionService.SubscribeEndpoint();
await s3Service.EnsureBucketExists();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();