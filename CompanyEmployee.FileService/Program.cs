using Amazon.SimpleNotificationService;
using CompanyEmployee.FileService.Messaging;
using CompanyEmployee.FileService.Services;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<MinioService>();
builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddControllers();
var awsConfig = builder.Configuration;

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
{
    return new AmazonSimpleNotificationServiceClient(
        awsConfig["AWS:AccessKey"],
        awsConfig["AWS:SecretKey"],
        new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = awsConfig["AWS:ServiceURL"]
        });
});
builder.Services.AddSingleton<SnsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var minio = scope.ServiceProvider.GetRequiredService<MinioService>();
        await minio.InitializeAsync();

        var snsSubscription = scope.ServiceProvider.GetRequiredService<SnsService>();
        await snsSubscription.SubscribeEndpoint();

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
app.MapControllers();
app.Run();