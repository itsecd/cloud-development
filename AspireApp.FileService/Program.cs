using Amazon.S3;
using Amazon.SQS;
using AspireApp.FileService.Messaging;
using AspireApp.FileService.Storage;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddHostedService<SqsConsumerService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var s3 = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3.EnsureBucketExists();
}

app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
