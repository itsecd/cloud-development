using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Contracts;
using DataGenerator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var snsServiceUrl = builder.Configuration["Sns:ServiceUrl"] ?? "http://localhost:4566";

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
{
    var config = new AmazonSimpleNotificationServiceConfig
    {
        ServiceURL = snsServiceUrl,
        AuthenticationRegion = "us-east-1"
    };
    var credentials = new BasicAWSCredentials("test", "test");
    return new AmazonSimpleNotificationServiceClient(credentials, config);
});

builder.Services.AddSingleton<SnsPublisher>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/generate/{count:int}", async (int count, SnsPublisher publisher) =>
{
    if (count <= 0 || count > 1000)
        return Results.BadRequest("Count must be between 1 and 1000");

    var employees = EmployeeGenerator.Generate(count);

    try
    {
        await publisher.PublishAsync(employees);
    }
    catch (Exception ex)
    {
        return Results.Problem("Failed to publish to SNS: " + ex.Message);
    }

    return Results.Ok(employees);
});

app.MapGet("/employees/{count:int}", (int count) =>
{
    if (count <= 0 || count > 1000)
        return Results.BadRequest("Count must be between 1 and 1000");

    var employees = EmployeeGenerator.Generate(count);
    return Results.Ok(employees);
});

app.MapGet("/", () => "DataGenerator Service is running");

app.Run();
