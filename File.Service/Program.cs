using Amazon.SQS;
using File.Service.Messaging;
using File.Service.Storage;
using LocalStack.Client.Extensions;
using VehicleApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddHostedService<SqsConsumerService>();

builder.AddMinioClient("vehicle-minio");
builder.Services.AddScoped<IFileStorage, MinioFileStorage>();

var app = builder.Build();

using var scope = app.Services.CreateScope();

var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
await storage.EnsureBucketExistsAsync();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
