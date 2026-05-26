using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<Vehicle?> RetrieveVehicleAsync(int id)
    {
        try
        {
            var cacheKey = $"car:{id}";
            var rawData = await _redis.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(rawData))
            {
                _logger.LogInformation("Кэш промах: автомобиль {Id} не найден", id);
                return null;
            }

            _logger.LogInformation("Кэш попадание: автомобиль {Id} загружен", id);
            return JsonSerializer.Deserialize<Vehicle>(rawData);
        }
        catch (Exception error)
        {
            _logger.LogWarning(error, "Ошибка при чтении из Redis для ID {Id}", id);
            return null;
        }
    }

    public async Task StoreVehicleAsync(Vehicle vehicle, int expirationMinutes)
    {
        try
        {
            var cacheKey = $"car:{vehicle.Id}";
            var jsonData = JsonSerializer.Serialize(vehicle);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
            };

            await _redis.SetStringAsync(cacheKey, jsonData, options);
            _logger.LogInformation("Автомобиль {Id} сохранён в кэш на {Minutes} минут", vehicle.Id, expirationMinutes);
        }
        catch (Exception error)
        {
            _logger.LogWarning(error, "Ошибка при сохранении в Redis для ID {Id}", vehicle.Id);
        }
    }
}