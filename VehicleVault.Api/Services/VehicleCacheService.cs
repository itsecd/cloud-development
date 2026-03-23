using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using VehicleVault.Api.Entities;

namespace VehicleVault.Api.Services;

/// <summary>
/// Сервис транспортных средств с кэшированием
/// </summary>
/// <param name="vehicleGenerator">Генератор транспортных средств</param>
/// <param name="distributedCache">Распределённый кэш</param>
/// <param name="appConfiguration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public class VehicleCacheService(
    IVehicleGeneratorService vehicleGenerator,
    IDistributedCache distributedCache,
    IConfiguration appConfiguration,
    ILogger<VehicleCacheService> logger) : IVehicleCacheService
{
    private readonly TimeSpan _entryLifetime = TimeSpan.FromMinutes(appConfiguration.GetValue("Cache:ExpirationMinutes", 5));

    /// <inheritdoc />
    public async Task<Vehicle> GetOrGenerate(int id)
    {
        var key = $"vehicle:{id}";

        var existing = await TryGetFromCache(key);
        if (existing is not null)
            return existing;

        logger.LogInformation("Cache miss for id {Id}, generating new vehicle", id);
        var result = vehicleGenerator.Generate(id);
        await TrySaveToCache(key, result);

        return result;
    }

    /// <summary>
    /// Попытка получить транспортное средство из кэша
    /// </summary>
    /// <param name="key">Ключ записи</param>
    /// <returns>Транспортное средство или null</returns>
    private async Task<Vehicle?> TryGetFromCache(string key)
    {
        try
        {
            var data = await distributedCache.GetStringAsync(key);
            if (data is null)
                return null;

            logger.LogInformation("Cache hit for key {Key}", key);
            return JsonSerializer.Deserialize<Vehicle>(data);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve from cache by key {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Попытка сохранить транспортное средство в кэш
    /// </summary>
    /// <param name="key">Ключ записи</param>
    /// <param name="vehicle">Транспортное средство</param>
    private async Task TrySaveToCache(string key, Vehicle vehicle)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(vehicle);
            await distributedCache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _entryLifetime
            });
            logger.LogInformation("Saved to cache with key {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save to cache by key {Key}", key);
        }
    }
}
