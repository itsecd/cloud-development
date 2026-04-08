using MedicalPatient.Generator.Services;
using MedicalPatient.AppHost.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("redis");

builder.Services.AddSingleton<MedicalPatientGenerator>();
builder.Services.AddScoped<MedicalPatientService>();


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