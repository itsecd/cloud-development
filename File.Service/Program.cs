using Amazon.S3;
using Amazon.SQS;
using File.Service.Messaging;
using File.Service.Storage;
using LocalStack.Client.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
    if (System.IO.File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddAwsService<IAmazonS3>();

builder.Services.AddScoped<IVehicleStorageService, S3VehicleStorageService>();
builder.Services.AddHostedService<SqsVehicleConsumerService>();

var app = builder.Build();

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();

var storage = scope.ServiceProvider.GetRequiredService<IVehicleStorageService>();
await storage.EnsureBucketExists();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
