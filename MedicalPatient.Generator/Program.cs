using MedicalPatient.Generator.Services;
using MedicalPatient.Generator;
using MedicalPatient.AppHost.ServiceDefaults;
using Serilog;
using Amazon.SQS;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");

builder.Services.AddSingleton<MedicalPatientGenerator>();
builder.Services.AddScoped<MedicalPatientService>();

var sqsServiceUrl = builder.Configuration["SQS:ServiceUrl"] ?? "http://localhost:4566";
var queueName = builder.Configuration["SQS:QueueName"] ?? "medical-patients";
var useLocalStack = builder.Configuration.GetValue<bool>("LocalStack:UseLocalStack");

builder.Services.AddMassTransit(x =>
{
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host("us-east-1", h =>
        {
            h.AccessKey("admin");
            h.SecretKey("admin");
            h.Config(new AmazonSQSConfig
            {
                ServiceURL = sqsServiceUrl,
                AuthenticationRegion = "us-east-1",
                UseHttp = true
            });
        });

        cfg.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType);

        cfg.ConfigureEndpoints(context);
    });
});

Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetry()
    .WriteTo.Console()
    .CreateLogger();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();


app.MapGet("/medicalpatient-generator", async (
    int id,
    MedicalPatientService service,
    ISendEndpointProvider sendEndpointProvider,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("A request was received for a company employee with the ID: {Id}", id);

    if (id <= 0)
    {
        logger.LogWarning("Invalid ID: {Id}", id);
        return Results.BadRequest(new { error = "ID must be > 0" });
    }

    try
    {
        var application = await service.GetByIdAsync(id, cancellationToken);

        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{queueName}"));
        await endpoint.Send(new MedicalPatientMessage(
            application.Id,
            application.FullName,
            application.Address,
            application.BirthDate,
            application.Height,
            application.Weight,
            application.BloodType,
            application.RhFactor,
            application.LastInspectionDate,
            application.VaccinationMark
        ), cancellationToken);

        return Results.Ok(application);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error when receiving data about a medical patient with {Id}", id);
        return Results.Problem("An error occurred while processing the request");
    }
})
.WithName("GetMedicalPatient");

app.Run();
