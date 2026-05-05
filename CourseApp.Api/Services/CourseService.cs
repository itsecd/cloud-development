using System.Text.Json;
using CourseApp.Api.Generators;
using CourseApp.Api.Messaging;
using CourseApp.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseApp.Api.Services;

/// <summary>
/// Сервис учебных курсов с кэшированием в Redis и публикацией в SQS
/// </summary>
/// <param name="cache">Распределённый кэш (Redis)</param>
/// <param name="producer">Продюсер сообщений (SQS)</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public sealed class CourseService(
    IDistributedCache cache,
    IProducerService producer,
    IConfiguration configuration,
    ILogger<CourseService> logger) : ICourseService
{
    private const string CacheKeyPrefix = "course:";

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(
        configuration.GetValue<int>("Cache:ExpirationMinutes"));

    /// <summary>
    /// Получение учебного курса по идентификатору с кэшированием и публикацией в очередь
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    public async Task<Course> GetCourse(int id)
    {
        var cachedCourse = await TryGetFromCache(id);
        if (cachedCourse is not null)
        {
            logger.LogInformation("Cache hit for course {Id}", id);
            return cachedCourse;
        }

        logger.LogInformation("Cache miss for course {Id}", id);
        var course = CourseGenerator.Generate(id);
        logger.LogInformation("Generated course {@Course}", course);

        await Task.WhenAll(TrySetToCache(id, course), producer.SendMessage(course));
        return course;
    }

    private async Task<Course?> TryGetFromCache(int id)
    {
        try
        {
            var data = await cache.GetStringAsync(CacheKeyPrefix + id);
            return data is null ? null : JsonSerializer.Deserialize<Course>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading course {Id} from cache", id);
            return null;
        }
    }

    private async Task TrySetToCache(int id, Course course)
    {
        try
        {
            var data = JsonSerializer.Serialize(course);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheExpiration };
            await cache.SetStringAsync(CacheKeyPrefix + id, data, options);
            logger.LogInformation("Course {Id} saved to cache", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving course {Id} to cache", id);
        }
    }
}
