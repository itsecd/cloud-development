using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using CreditApplication.Generator.Services;
using CreditApplication.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");

// AWS SNS client for publishing to LocalStack
var awsServiceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
    new AmazonSimpleNotificationServiceClient(
        new BasicAWSCredentials("test", "test"),
        new AmazonSimpleNotificationServiceConfig { ServiceURL = awsServiceUrl }));

builder.Services.AddSingleton<SnsPublisherService>();
builder.Services.AddSingleton<CreditApplicationGenerator>();
builder.Services.AddScoped<CreditApplicationService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

// API endpoint для получения кредитной заявки
app.MapGet("/credit-application", async (
    int id,
    CreditApplicationService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (id <= 0)
    {
        logger.LogWarning("Received invalid ID: {Id}", id);
        return Results.BadRequest(new { error = "ID must be a positive number" });
    }

    try
    {
        var application = await service.GetByIdAsync(id, cancellationToken);
        return Results.Ok(application);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while getting credit application {Id}", id);
        return Results.Problem("An error occurred while processing the request");
    }
})
.WithName("GetCreditApplication");

app.Run();
