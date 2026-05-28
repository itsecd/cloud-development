using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Реализация сервиса кэширования транспортных средств с помощью Redis и отправкой в SQS
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IVehiclePublisherService _vehiclePublisher;

    public RedisCacheService(
        IDistributedCache redis,
        ILogger<RedisCacheService> logger,
        IVehiclePublisherService vehiclePublisher)
    {
        _redis = redis;
        _logger = logger;
        _vehiclePublisher = vehiclePublisher;
    }

    public async Task<Vehicle?> RetrieveVehicleAsync(int id)
    {
        try
        {
            var cacheKey = $"car:{id}";
            var rawData = await _redis.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(rawData))
            {
                _logger.LogInformation("Cache miss: vehicle {Id} not found", id);
                return null;
            }

            _logger.LogInformation("Cache hit: vehicle {Id} loaded", id);
            return JsonSerializer.Deserialize<Vehicle>(rawData);
        }
        catch (Exception error)
        {
            _logger.LogWarning(error, "Error reading from Redis for ID {Id}", id);
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
            _logger.LogInformation("Vehicle {Id} saved to cache for {Minutes} minutes", vehicle.Id, expirationMinutes);

            await _vehiclePublisher.SendVehicleToQueueAsync(vehicle);
        }
        catch (Exception error)
        {
            _logger.LogWarning(error, "Error saving to Redis for ID {Id}", vehicle.Id);
        }
    }
}