using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Interfaces;

/// <summary>
/// Контракт прикладного сервиса генерации контрактов с учетом кэша.
/// </summary>
public interface ICourseContractsService
{
    /// <summary>
    /// Возвращает список контрактов из кэша или генерирует новые.
    /// </summary>
    /// <param name="count">Количество требуемых контрактов.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список контрактов.</returns>
    Task<IReadOnlyList<CourseContract>> GenerateAsync(int count, CancellationToken cancellationToken = default);
}
