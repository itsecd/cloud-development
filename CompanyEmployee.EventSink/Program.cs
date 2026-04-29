using Amazon.SimpleNotificationService;
using CompanyEmployee.EventSink.Messaging;
using CompanyEmployee.EventSink.S3;
using CompanyEmployee.ServiceDefaults;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.AddMinioClient("minio");

builder.Services.AddSingleton<SnsSubscriptionService>();
builder.Services.AddSingleton<IS3Service, S3MinioService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var snsService = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();

    await snsService.SubscribeEndpoint();
    await s3Service.EnsureBucketExists();
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();