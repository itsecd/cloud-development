using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using FileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var serviceUrl = builder.Configuration["AWS:ServiceURL"];

if (!string.IsNullOrEmpty(serviceUrl))
{
    var credentials = new BasicAWSCredentials("test", "test");

    builder.Services.AddSingleton<IAmazonS3>(_ =>
        new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        }));

    builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
        new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = serviceUrl
        }));
}
else
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonS3>();
    builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
}

builder.Services.AddSingleton<IS3FileStorageService, S3FileStorageService>();
builder.Services.AddHostedService<SnsSubscriptionHostedService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
