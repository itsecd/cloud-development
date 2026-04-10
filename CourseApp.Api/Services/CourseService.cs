using System.Text.Json;
using CourseApp.Api.Generators;
using CourseApp.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CourseApp.Api.Services;

/// <summary>
/// Сервис учебных курсов с кэшированием в Redis
/// </summary>
/// <param name="cache">Распределённый кэш</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public sealed class CourseService(
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<CourseService> logger) : ICourseService
{
    private const string CacheKeyPrefix = "course:";

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(
        configuration.GetValue<int>("Cache:ExpirationMinutes"));

    /// <summary>
    /// Получение учебного курса по идентификатору с кэшированием
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    public async Task<Course> GetCourse(int id)
    {
        var cachedCourse = await TryGetFromCache(id);

        if (cachedCourse is not null)
        {
            logger.LogInformation("Cache hit for course with id {Id}", id);
            return cachedCourse;
        }

        logger.LogInformation("Cache miss for course with id {Id}", id);

        var course = CourseGenerator.Generate(id);
        logger.LogInformation("Generated course {@Course}", course);

        await TrySetToCache(id, course);

        return course;
    }

    private async Task<Course?> TryGetFromCache(int id)
    {
        try
        {
            var key = CacheKeyPrefix + id;
            var data = await cache.GetStringAsync(key);

            if (data is null)
                return null;

            return JsonSerializer.Deserialize<Course>(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading course with id {Id} from cache", id);
            return null;
        }
    }

    private async Task TrySetToCache(int id, Course course)
    {
        try
        {
            var key = CacheKeyPrefix + id;
            var data = JsonSerializer.Serialize(course);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            await cache.SetStringAsync(key, data, options);

            logger.LogInformation("Course with id {Id} saved to cache", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving course with id {Id} to cache", id);
        }
    }
}
