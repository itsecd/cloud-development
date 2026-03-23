using VehicleVault.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddSingleton<IVehicleGeneratorService, VehicleGeneratorService>();
builder.Services.AddSingleton<IVehicleCacheService, VehicleCacheService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET");
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();

app.MapGet("api/vehicle", async (int id, IVehicleCacheService vehicleService) =>
    await vehicleService.GetOrGenerate(id));

app.Run();
