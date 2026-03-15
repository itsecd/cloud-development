using TrainingCourse.Api.Models;

namespace TrainingCourse.Api.Services;

/// <summary>
/// Интерфейс сервиса для работы с курсами
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Получить курс по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Модель курса</returns>
    public Task<Course> GetCourse(int id);
}