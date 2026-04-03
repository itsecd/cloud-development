using Amazon.S3;
using Amazon.SQS;
using CreditApp.FileService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var localstackUrl = builder.Configuration["LOCALSTACK_URL"] ?? "http://localhost:4566";

var s3Config = new AmazonS3Config
{
    ServiceURL = localstackUrl,
    ForcePathStyle = true,
    UseHttp = true
};
builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client("test", "test", s3Config));

var sqsConfig = new AmazonSQSConfig
{
    ServiceURL = localstackUrl,
    UseHttp = true
};
builder.Services.AddSingleton<IAmazonSQS>(new AmazonSQSClient("test", "test", sqsConfig));

builder.Services.AddScoped<IFileStorage, S3Storage>();
builder.Services.AddHostedService<SqsConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();