using CompanyEmployee.FileService.Services;
using CompanyEmployee.ServiceDefaults;
using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.AddMinioClient("minio");

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

var bucketName = builder.Configuration["MinIO:BucketName"]
    ?? throw new InvalidOperationException("MinIO:BucketName is not configured");
builder.Services.AddSingleton<IStorageService, MinioStorageService>();
builder.Services.AddSingleton(bucketName);
builder.Services.AddHostedService<SnsInitializerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

using (var scope = app.Services.CreateScope())
{
    var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await storageService.EnsureBucketExistsAsync(bucketName);
        logger.LogInformation("Storage initialized successfully for bucket {BucketName}", bucketName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize storage bucket {BucketName}", bucketName);
        throw;
    }
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();