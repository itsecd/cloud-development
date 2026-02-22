using Generator.Generator;
using Generator.Service;
using ResidentialBuilding.ServiceDefaults;
using Serilog;
using Serilog.Formatting.Compact;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .ReadFrom.Configuration(hostingContext.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            new CompactJsonFormatter()
        );
});

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("residential-building-cache");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<ResidentialBuildingGenerator>();
builder.Services.AddSingleton<IResidentialBuildingService, ResidentialBuildingService>();

builder.Services.AddControllers();

WebApplication app = builder.Build();

app.UseCors("AllowLocalDev");

app.MapControllers();

app.Run();