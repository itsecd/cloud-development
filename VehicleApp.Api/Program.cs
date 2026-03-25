using VehicleApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddScoped<IVehicleService, VehicleService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/vehicles", async (int id, IVehicleService vehicleService) =>
{
    var vehicle = await vehicleService.GetVehicle(id);
    return Results.Ok(vehicle);
});

app.Run();
