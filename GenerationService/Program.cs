using GenerationService.Endpoints;
using GenerationService.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Debug()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — структурное логирование
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    );

    // Aspire ServiceDefaults (OpenTelemetry, health checks, service discovery)
    builder.AddServiceDefaults();

    // Redis Distributed Cache через Aspire
    builder.AddRedisDistributedCache("cache");

    // Сервис генерации данных
    builder.Services.AddSingleton<ProjectGeneratorService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Generation Service API",
            Version = "v1",
            Description = "Сервис генерации программных проектов. Вариант 39."
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapDefaultEndpoints();
    app.MapProjectsEndpoints();

    Log.Information("GenerationService started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "GenerationService crashed on startup");
}
finally
{
    Log.CloseAndFlush();
}
