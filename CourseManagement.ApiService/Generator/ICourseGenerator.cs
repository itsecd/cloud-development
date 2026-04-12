using CourseManagement.ApiService.Entities;

namespace CourseManagement.ApiService.Generator;

/// <summary>
/// Интерфейс генератора для сущности типа Курс
/// </summary>
public interface ICourseGenerator
{
    /// <summary>
    /// Метод для генерации одного экземпляра сущности типа Курс
    /// </summary>
    /// <param name="id">Идентификатор курса (если не указан, генерируется автоматически)</param>
    /// <returns>Сгенерированный курс</returns>
    public Course GenerateOne(int? id = null);
}
