using VehicleApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddCors(options =>
    options.AddPolicy("AllowClient", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? [])
              .WithMethods("GET")
              .AllowAnyHeader()));

var app = builder.Build();

app.UseCors("AllowClient");
app.MapDefaultEndpoints();

app.MapGet("/vehicles", async (int id, IVehicleService vehicleService) =>
{
    var vehicle = await vehicleService.GetVehicleAsync(id);
    return Results.Ok(vehicle);
});

app.Run();
