using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Vehicle.Api.Entities;

namespace Vehicle.Api.Cache;

/// <summary>
/// Класс для работы с Redis-кэшем транспортных средств.
/// </summary>
public class RedisVehicleCache(IDistributedCache cache) : IVehicleCache
{
    /// <summary>
    /// Получает один объект из кэша по ключу.
    /// </summary>
    /// <param name="key">Ключ кэша.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Объект или null, если данных нет.</returns>
    public async Task<VehicleEntity?> GetOneAsync(string key, CancellationToken cancellationToken = default)
    {
        var json = await cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<VehicleEntity>(json);
    }

    /// <summary>
    /// Сохраняет один объект в кэш.
    /// </summary>
    /// <param name="key">Ключ кэша.</param>
    /// <param name="vehicle">Объект транспортного средства.</param>
    /// <param name="ttl">Время хранения в кэше.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task SetOneAsync(
        string key,
        VehicleEntity vehicle,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(vehicle);

        await cache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);
    }
}