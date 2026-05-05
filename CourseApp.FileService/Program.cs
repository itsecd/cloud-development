using Amazon.S3;
using Amazon.SQS;
using CourseApp.FileService.Messaging;
using CourseApp.FileService.Storage;
using LocalStack.Client.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSwaggerGen(options =>
{
    var assembly = Assembly.GetExecutingAssembly();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml"));
});

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddHostedService<SqsConsumerService>();

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.MapControllers();
app.Run();
