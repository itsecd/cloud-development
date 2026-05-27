using VehicleGen.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVehicleGenerator, VehicleGenerator>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.AddRedisDistributedCache("cache");

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

app.UseCors();
app.UseHttpsRedirection();

app.MapGet("/api/vehicle", async (int id, IVehicleService vehicleService) =>
{
    try
    {
        var vehicle = await vehicleService.GetOrCreateVehicleAsync(id);
        return Results.Ok(vehicle);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();