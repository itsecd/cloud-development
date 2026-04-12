using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseManagement.ApiService.Cache;

/// <summary>
/// Универсальный сервис для взаимодействия с кэшем
/// </summary>
/// <param name="logger">Логгер</param>
/// <param name="cache">Кэш</param>
public class CacheService<T>(ILogger<CacheService<T>> logger, IDistributedCache cache) : ICacheService<T>
{
    /// <summary>
    /// Время жизни данных в кэше
    /// </summary>
    private static readonly double _cacheDuration = 5;

    /// <inheritdoc/>
    public async Task<T?> FetchAsync(string key, int id)
    {
        var cacheKey = $"{key}:{id}";

        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var obj = JsonSerializer.Deserialize<T>(cached);

                logger.LogInformation("Cache hit for {EntityType} {ResourceId}", typeof(T).Name, id);

                return obj;
            }

            logger.LogInformation("Cache miss for {EntityType} {ResourceId}", typeof(T).Name, id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сache is unavailable");
        }

        return default;
    }

    /// <inheritdoc/>
    public async Task StoreAsync(string key, int id, T entity)
    {
        var cacheKey = $"{key}:{id}";

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheDuration)
        };

        try
        {
            var serialized = JsonSerializer.Serialize(entity);
            await cache.SetStringAsync(cacheKey, serialized, options);
            logger.LogInformation("{EntityType} {ResourceId} cached", typeof(T).Name, id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сache is unavailable");
        }
    }
}
