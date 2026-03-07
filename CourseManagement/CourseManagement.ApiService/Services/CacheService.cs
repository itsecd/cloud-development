using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseManagement.ApiService.Services;

/// <summary>
/// Сервис для взаимодействия с кэшем
/// </summary>
/// <param name="cache">Кэш</param>
/// <param name="logger">Логгер</param>
public class CacheService<T>(ILogger<CacheService<T>> logger, IDistributedCache cache)
{
    /// <summary>
    /// Время жизни данных в кэше
    /// </summary>
    private static readonly double _cacheDuration = 5;

    /// <summary>
    /// Асинхронный метод для извлечения данных из кэша
    /// </summary>
    /// <param name="key">Ключ для сущности</param>
    /// <param name="id">Идентификатор объекта</param>
    /// <returns>Объект или null при его отсутствии</returns>
    public async Task<T?> FetchAsync(string key, int id)
    {
        var cacheKey = $"{key}:{id}";

        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var obj = JsonSerializer.Deserialize<T>(cached);

                logger.LogInformation("Cache hit for object {ResourceId}", id);

                return obj;
            }

            logger.LogInformation("Cache miss for object {ResourceId}", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сache is unavailable");
        }

        return default;
    }

    /// <summary>
    /// Асинхронный метод для внесения данных в кэш
    /// </summary>
    /// <param name="key">Ключ для сущности</param>
    /// <param name="id">Идентификатор объекта</param>
    /// <param name="obj">Объект</param>
    public async Task StoreAsync(string key, int id, T obj)
    {
        var cacheKey = $"{key}:{id}";

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheDuration)
        };

        try
        {
            var serialized = JsonSerializer.Serialize(obj);
            await cache.SetStringAsync(cacheKey, serialized, options);
            logger.LogInformation("Object {ResourceId} cached", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сache is unavailable");
        }

    }
}
