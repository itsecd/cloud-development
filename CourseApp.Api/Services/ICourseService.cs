using CourseApp.Api.Models;

namespace CourseApp.Api.Services;

/// <summary>
/// Интерфейс сервиса учебных курсов
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Получение учебного курса по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    public Task<Course> GetCourse(int id);
}
