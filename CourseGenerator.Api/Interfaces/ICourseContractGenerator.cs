using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Interfaces;

/// <summary>
/// Контракт генератора учебных контрактов.
/// </summary>
public interface ICourseContractGenerator
{
    /// <summary>
    /// Генерирует указанное количество учебных контрактов.
    /// </summary>
    /// <param name="count">Количество элементов для генерации.</param>
    /// <returns>Список сгенерированных контрактов.</returns>
    IReadOnlyList<CourseContract> Generate(int count);
}
