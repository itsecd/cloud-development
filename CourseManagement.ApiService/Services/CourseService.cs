using CourseManagement.ApiService.Cache;
using CourseManagement.ApiService.Entities;
using CourseManagement.ApiService.Generator;
using CourseManagement.ApiService.Messaging;

namespace CourseManagement.ApiService.Services;

/// <summary>
/// Сервис для сущности типа Курс
/// </summary>
/// <param name="logger">Логгер</param>
/// <param name="generator">Генератор курсов</param>
/// <param name="cacheService">Сервис для взаимодействия с кэшем</param>
/// <param name="publisherService">Сервис для отправки сообщения в брокер</param>
public class CourseService(ILogger<CourseService> logger, ICourseGenerator generator, ICacheService<Course> cacheService, IPublisherService<Course> publisherService) : ICourseService
{
    /// <summary>
    /// Константа для ключа кэша
    /// </summary>
    private const string CacheKeyPrefix = "course";

    /// <inheritdoc/>
    public async Task<Course> GetCourse(int id)
    {
        var course = await cacheService.FetchAsync(CacheKeyPrefix, id);
        if (course != null)
            return course;

        var newCourse = generator.GenerateOne(id);

        await publisherService.SendMessage(id, newCourse);
        await cacheService.StoreAsync(CacheKeyPrefix, id, newCourse);

        logger.LogInformation("Course {ResourceId} processed", id);

        return newCourse;
    }
}
