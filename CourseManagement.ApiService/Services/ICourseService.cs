using CourseManagement.ApiService.Entities;

namespace CourseManagement.ApiService.Services;

/// <summary>
/// Интерфейс сервиса для работы с сущностью Курс
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Метод для получения курса
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Курс</returns>
    public Task<Course> GetCourse(int id);
}