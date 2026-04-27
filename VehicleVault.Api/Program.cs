using VehicleVault.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddSingleton<IVehicleGeneratorService, VehicleGeneratorService>();
builder.Services.AddSingleton<IVehicleCacheService, VehicleCacheService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("api/vehicle", async (int id, IVehicleCacheService vehicleService) =>
    await vehicleService.GetOrGenerate(id));

app.Run();
