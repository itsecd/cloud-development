using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VehicleApp.Api.Generators;
using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services;

/// <summary>
/// Сервис получения транспортных средств с кэшированием
/// </summary>
/// <param name="cache">Распределённый кэш</param>
/// <param name="logger">Логгер</param>
/// <param name="configuration">Конфигурация приложения</param>
public sealed class VehicleService(IDistributedCache cache, ILogger<VehicleService> logger, IConfiguration configuration) : IVehicleService
{
    private const string KeyPrefix = "vehicle:";

    private readonly TimeSpan _entryLifetime = TimeSpan.FromMinutes(
        configuration.GetValue("CacheExpirationMinutes", 15));

    /// <inheritdoc />
    public async Task<Vehicle> GetOrGenerateAsync(int id)
    {
        var key = $"{KeyPrefix}{id}";

        var cached = await TryGetFromCacheAsync(key);
        if (cached is not null)
            return cached;

        var vehicle = VehicleGenerator.Generate(id);
        logger.LogInformation("Vehicle generated. Id: {VehicleId}", id);

        await SetToCacheAsync(key, vehicle);
        return vehicle;
    }

    /// <summary>
    /// Попытаться получить транспортное средство из кэша
    /// </summary>
    /// <param name="key">Ключ кэша</param>
    /// <returns>Транспортное средство или <see langword="null"/> при промахе или ошибке кэша</returns>
    private async Task<Vehicle?> TryGetFromCacheAsync(string key)
    {
        logger.LogInformation("Getting vehicle from cache. Key: {CacheKey}", key);
        try
        {
            var json = await cache.GetStringAsync(key);
            if (json is null)
            {
                logger.LogInformation("Cache miss. Key: {CacheKey}", key);
                return null;
            }

            logger.LogInformation("Cache hit. Key: {CacheKey}", key);
            return JsonSerializer.Deserialize<Vehicle>(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get vehicle from cache. Key: {CacheKey}", key);
            return null;
        }
    }

    /// <summary>
    /// Сохранить транспортное средство в кэш
    /// </summary>
    /// <param name="key">Ключ кэша</param>
    /// <param name="vehicle">Транспортное средство для сохранения</param>
    private async Task SetToCacheAsync(string key, Vehicle vehicle)
    {
        logger.LogInformation("Saving vehicle to cache. Key: {CacheKey}", key);
        try
        {
            var json = JsonSerializer.Serialize(vehicle);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _entryLifetime
            };
            await cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save vehicle to cache. Key: {CacheKey}", key);
        }
    }
}
