using Amazon.SimpleNotificationService;
using File.Service.Messaging;
using File.Service.Storage;
using LocalStack.Client.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSwaggerGen(options =>
{
    var assembly = Assembly.GetExecutingAssembly();
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml"));
});

builder.Services.AddControllers();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();
builder.Services.AddScoped<SnsSubscriptionService>();

builder.AddMinioClient("vehicle-minio");
builder.Services.AddScoped<IS3Service, MinioS3Service>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();

var s3 = scope.ServiceProvider.GetRequiredService<IS3Service>();
await s3.EnsureBucketExists();
var subscription = scope.ServiceProvider.GetRequiredService<SnsSubscriptionService>();
await subscription.SubscribeEndpoint();


app.MapControllers();
app.Run();
