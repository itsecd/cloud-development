using Amazon.S3;
using Amazon.SimpleNotificationService;
using Inventory.FileService.Messaging;
using Inventory.FileService.Storage;
using Inventory.ServiceDefaults;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// LocalStack
builder.Services.AddLocalStack(builder.Configuration);

// AWS services
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

// App services
builder.Services.AddSingleton<IS3Service, S3AwsService>();
builder.Services.AddSingleton<ISubscriberService, SnsSubscriberService>();

var app = builder.Build();

app.Logger.LogInformation("AWS Region: {Region}", builder.Configuration["AWS:Region"]);
app.Logger.LogInformation("S3 Bucket: {Bucket}", builder.Configuration["AWS:Resources:S3BucketName"]);
app.Logger.LogInformation("SNS TopicArn: {TopicArn}", builder.Configuration["AWS:Resources:SNSTopicArn"]);
app.Logger.LogInformation("SNS EndpointURL: {Endpoint}", builder.Configuration["SNS:EndpointURL"]);

using (var scope = app.Services.CreateScope())
{
    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3Service.EnsureBucketExists();

    var subscriberService = scope.ServiceProvider.GetRequiredService<ISubscriberService>();
    await subscriberService.SubscribeEndpoint();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();