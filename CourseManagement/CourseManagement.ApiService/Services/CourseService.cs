using CourseManagement.ApiService.Dto;

namespace CourseManagement.ApiService.Services;

/// <summary>
/// Сервис для сущности типа Курс
/// </summary>
/// <param name="generator">Генератор курсов</param>
/// <param name="logger">Логгер</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="cacheService">Сервис для взаимодействия с кэшем</param>
public class CourseService(CourseGenerator generator, ILogger<CourseService> logger, IConfiguration configuration, CacheService<CourseDto> cacheService)
{
    /// <summary>
    /// Метод для получения курса
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Курс или null при ошибке</returns>
    public async Task<CourseDto?> GetCourse(int id)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Processing request for course {ResourceId}", id);

            var course = await cacheService.FetchAsync("course", id);
            if (course != null)
                return course;

            var newCourse = generator.GenerateOne(id);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Course {ResourceId} generated", id);

            var cacheDuration = configuration.GetValue<double?>("Cache:DurationMinutes") ?? 5;

            await cacheService.StoreAsync("course", id, newCourse, cacheDuration);

            return newCourse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing course {ResourceId}", id);
        }

        return null;
    }
}
