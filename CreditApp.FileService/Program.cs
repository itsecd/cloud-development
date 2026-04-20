using Amazon.S3;
using Amazon.SQS;
using CreditApp.FileService.Services;
using CreditApp.ServiceDefaults;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddAwsService<IAmazonSQS>();

builder.Services.AddScoped<IFileStorage, S3Storage>();
builder.Services.AddHostedService<SqsConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
    await storage.EnsureBucketExists();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
