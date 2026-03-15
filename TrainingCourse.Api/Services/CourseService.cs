namespace TrainingCourse.Api.Services;

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TrainingCourse.Api.Models;

/// <summary>
/// Сервис для получения информации о курсе
/// </summary>
public class CourseService(IDistributedCache cache, IConfiguration configuration,
                ILogger<CourseService> logger) : ICourseService
{
    private readonly int _expirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    /// <inheritdoc />
    public async Task<Course> GetCourse(int id)
    {
        var cacheKey = $"course-{id}";
        logger.LogInformation("Requesting course {CourseId} from cache", id);
        var cachedData = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                var cachedCourse = JsonSerializer.Deserialize<Course>(cachedData);

                if (cachedCourse != null)
                {
                    logger.LogInformation("Course {CourseId} retrieved from cache", id);
                    return cachedCourse;
                }
                logger.LogWarning("Course {CourseId} found in cache but deserialization returned null", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize course {CourseId} from cache", id);
            }
        }

        logger.LogInformation("Course {CourseId} not found in cache. Generating", id);

        var course = CourseGenerator.GenerateCourse(id);

        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expirationMinutes)
            };
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(course), cacheOptions);
            logger.LogInformation("Course {CourseId} generated and cached", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache course {CourseId}. Continuing without cache.", id);
        }
        return course;
    }
}