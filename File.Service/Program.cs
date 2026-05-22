using Amazon.SQS;
using File.Service.Messaging;
using File.Service.Storage;
using LocalStack.Client.Extensions;
using Patient.ServiceDefaults;
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
builder.Services.AddHostedService<SqsConsumerService>();

builder.AddMinioClient("patient-minio");
builder.Services.AddScoped<IS3Service, S3MinioService>();

var app = builder.Build();

using var scope = app.Services.CreateScope();

var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
await s3Service.EnsureBucketExists();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
