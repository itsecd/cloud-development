using CourseManagement.ApiService.Dto;

namespace CourseManagement.ApiService.Services;

/// <summary>
/// Сервис для сущности типа Курс
/// </summary>
/// <param name="logger">Логгер</param>
/// <param name="generator">Генератор курсов</param>
/// <param name="cacheService">Сервис для взаимодействия с кэшем</param>
public class CourseService(ILogger<CourseService> logger, CourseGenerator generator, CacheService<CourseDto> cacheService)
{
    /// <summary>
    /// Константа для ключа кэша
    /// </summary>
    private const string CacheKeyPrefix = "course";

    /// <summary>
    /// Метод для получения курса
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Курс или null при ошибке</returns>
    public async Task<CourseDto?> GetCourse(int id)
    {
        try
        {
            var course = await cacheService.FetchAsync(CacheKeyPrefix, id);
            if (course != null)
                return course;

            var newCourse = generator.GenerateOne(id);

            await cacheService.StoreAsync(CacheKeyPrefix, id, newCourse);

            return newCourse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing course {ResourceId}", id);
        }

        return null;
    }
}
