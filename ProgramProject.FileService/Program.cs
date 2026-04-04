using Amazon.Runtime;
using Amazon.SQS;
using Minio;
using ProgramProject.FileService.Services;
using ProgramProject.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

Console.OutputEncoding = System.Text.Encoding.UTF8;

builder.AddServiceDefaults();

// Настройка SQS
var sqsConfig = new AmazonSQSConfig
{
    ServiceURL = builder.Configuration["SQS:ServiceURL"] ?? "http://127.0.0.1:59517",
    UseHttp = true,
    AuthenticationRegion = "us-east-1"
};
builder.Services.AddSingleton<IAmazonSQS>(sp =>
    new AmazonSQSClient(new AnonymousAWSCredentials(), sqsConfig));

// Настройка Minio
builder.Services.AddSingleton<Minio.IMinioClient>(sp =>
{
    var endpoint = builder.Configuration["Minio:Endpoint"] ?? "localhost:9000";
    var accessKey = builder.Configuration["Minio:AccessKey"] ?? "minioadmin";
    var secretKey = builder.Configuration["Minio:SecretKey"] ?? "minioadmin";

    return new Minio.MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey)
        .WithSSL(false)
        .Build();
});

// Регистрируем фоновый сервис
builder.Services.AddHostedService<SqsBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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