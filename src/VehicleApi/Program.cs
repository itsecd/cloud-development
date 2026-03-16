using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VehicleApi.Models;
using VehicleApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("cache");

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

app.MapDefaultEndpoints();

app.MapGet("/api/vehicles", async (int id, IDistributedCache cache, ILogger<Program> logger) =>
{
    if (id <= 0)
    {
        logger.LogWarning("Invalid vehicle ID {Id} requested", id);
        return Results.BadRequest("ID must be greater than 0");
    }

    var cacheKey = $"vehicle:{id}";
    var cachedData = await cache.GetAsync(cacheKey);

    if (cachedData != null)
    {
        logger.LogInformation("Cache hit for vehicle ID {Id}", id);
        var vehicle = JsonSerializer.Deserialize<Vehicle>(cachedData);
        logger.LogInformation("Returning cached vehicle: {@Vehicle}", vehicle);
        return Results.Ok(vehicle);
    }

    logger.LogInformation("Cache miss for vehicle ID {Id}", id);
    var generated = VehicleGenerator.Generate(id);
    logger.LogInformation("Generated new vehicle: {@Vehicle}", generated);

    var serialized = JsonSerializer.SerializeToUtf8Bytes(generated);
    await cache.SetAsync(cacheKey, serialized, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    });
    logger.LogInformation("Vehicle {Id} cached for 10 minutes", id);

    return Results.Ok(generated);
});

app.Run();
