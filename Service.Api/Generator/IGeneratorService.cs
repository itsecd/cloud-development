using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Интерфейс для запуска юзкейса по обработке учебных курсов
/// </summary>
public interface IGeneratorService
{
    /// <summary>
    /// Обработка запроса на генерацию учебного курса.
    /// </summary>
    /// <param name="id">ИД</param>
    /// <returns></returns>
    public Task<StudyCourse> ProcessCourse(int id);
}
