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
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] tid={ThreadId} | {Message:lj}{NewLine}{Exception}"));

    var redisConnection = builder.Configuration.GetConnectionString("redis")
        ?? throw new InvalidOperationException("Не задана строка подключения 'redis'");

    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);
    builder.Services.AddScoped<PatientService>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(o =>
        o.SwaggerDoc("v1", new() { Title = "GeneratorService — Medical Patient", Version = "v1" }));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseSwagger();
    app.UseSwaggerUI();

    // GET /patient?id=42
    app.MapGet("/patient", async (int id, PatientService svc, CancellationToken ct) =>
        id <= 0
            ? Results.BadRequest("id must be > 0")
            : Results.Ok(await svc.GetAsync(id, ct)))
        .WithName("GetPatient")
        .Produces<MedicalPatient>()
        .ProducesProblem(400);

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
