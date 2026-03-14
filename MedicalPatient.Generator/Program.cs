using MedicalPatient.Generator.Services;
using MedicalPatient.AppHost.ServiceDefaults;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.AddServiceDefaults();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
    options.InstanceName = "medical-patient:";
});

builder.Services.AddSingleton<MedicalPatientGenerator>();
builder.Services.AddScoped<MedicalPatientService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

app.MapGet("/medicalpatient-generator", async (
    int id,
    MedicalPatientService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Получен запрос на сотрудника компании с ID: {Id}", id);

    if (id <= 0)
    {
        logger.LogWarning("Неверный ID: {Id}", id);
        return Results.BadRequest(new { error = "ID должен быть > 0" });
    }

    try
    {
        var application = await service.GetByIdAsync(id, cancellationToken);
        return Results.Ok(application);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при получении данных о медицинском пациенте с {Id}", id);
        return Results.Problem("При обработке запроса произошла ошибка");
    }
})
.WithName("GetMedicalPatient");

app.Run();