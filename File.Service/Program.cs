using Amazon.S3;
using Amazon.SQS;
using File.Service.Messaging;
using File.Service.Storage;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddAwsService<IAmazonSQS>();

builder.Services.AddScoped<IVehicleStorageService, S3VehicleStorageService>();
builder.Services.AddHostedService<SqsVehicleConsumerService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<IVehicleStorageService>();
    await storage.PrepareBucketAsync();
    await storage.PrepareQueueAsync();
}

app.MapControllers();
app.Run();