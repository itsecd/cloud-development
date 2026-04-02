using GeneratorService.Models;
using GeneratorService.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] tid={ThreadId} | {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, _, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] tid={ThreadId} | {Message:lj}{NewLine}{Exception}")
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = ctx.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "generator-service"
            };
        }));

    builder.AddRedisDistributedCache("redis");

    builder.Services.AddScoped<PatientService>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(o =>
    {
        o.SwaggerDoc("v1", new() { Title = "GeneratorService — Medical Patient", Version = "v1" });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            o.IncludeXmlComments(xmlPath);
    });
    builder.Services.AddCors();

    builder.AddServiceDefaults();

    var app = builder.Build();

    app.MapDefaultEndpoints();

    var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]
        ?? throw new InvalidOperationException("Cors:AllowedOrigin is not configured");

    app.UseSerilogRequestLogging();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(policy => policy
        .WithOrigins(allowedOrigin)
        .AllowAnyMethod()
        .AllowAnyHeader());

    app.MapGet("/patient", async (int id, PatientService svc, CancellationToken ct) =>
        id <= 0
            ? Results.BadRequest("id must be > 0")
            : Results.Ok(await svc.GetAsync(id, ct)))
        .WithName("GetPatient")
        .WithSummary("Возвращает медицинскую карту пациента по идентификатору")
        .Produces<MedicalPatient>()
        .ProducesProblem(400);

    app.Logger.LogInformation("CORS AllowedOrigin = {Origin}",
    builder.Configuration["Cors:AllowedOrigin"] ?? "NOT SET");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}