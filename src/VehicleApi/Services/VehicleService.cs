using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VehicleApi.Models;

namespace VehicleApi.Services;

public class VehicleService(IDistributedCache cache, ILogger<VehicleService> logger)
{
    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public async Task<Vehicle> GetByIdAsync(int id)
    {
        var cacheKey = $"vehicle:{id}";
        var cachedData = await cache.GetAsync(cacheKey);

        if (cachedData != null)
        {
            logger.LogInformation("Cache hit for vehicle ID {Id}", id);
            return JsonSerializer.Deserialize<Vehicle>(cachedData)!;
        }

        logger.LogInformation("Cache miss for vehicle ID {Id}", id);
        var vehicle = VehicleGenerator.Generate(id);

        var serialized = JsonSerializer.SerializeToUtf8Bytes(vehicle);
        await cache.SetAsync(cacheKey, serialized, _cacheOptions);
        logger.LogInformation("Vehicle {Id} cached for 10 minutes", id);

        return vehicle;
    }
}
