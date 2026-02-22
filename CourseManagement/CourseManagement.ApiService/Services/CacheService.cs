using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseManagement.ApiService.Services;

/// <summary>
/// Сервис для взаимодействия с кэшем
/// </summary>
/// <param name="cache">Кэш</param>
/// <param name="logger">Логгер</param>
public class CacheService<T>(IDistributedCache cache, ILogger<CacheService<T>> logger)
{
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

                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Cache hit for object {ResourceId}. Object data: {@Object}", id, obj);

                return obj;
            }

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Cache miss for object {ResourceId}, generating new", id);
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
    /// <param name="cacheDuration">Время жизни данных в кэше</param>
    public async Task StoreAsync(string key, int id, T obj, double cacheDuration)
    {
        var cacheKey = $"{key}:{id}";

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheDuration)
        };

        try
        {
            var serialized = JsonSerializer.Serialize(obj);
            await cache.SetStringAsync(cacheKey, serialized, options);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Object {ResourceId} cached. Object details: {@Object}", id, obj);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сache is unavailable");
        }

    }
}
