using CourseGenerator.Api.Models;

namespace CourseGenerator.Api.Interfaces;

/// <summary>
/// Контракт сервиса кэширования сгенерированных учебных контрактов.
/// </summary>
public interface ICourseContractCacheService
{
    /// <summary>
    /// Возвращает список контрактов из кэша по размеру выборки.
    /// </summary>
    /// <param name="count">Количество контрактов в запрошенной выборке.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Список контрактов из кэша или null, если запись не найдена.</returns>
    Task<IReadOnlyList<CourseContract>?> GetAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет список контрактов в кэш.
    /// </summary>
    /// <param name="count">Количество контрактов в выборке.</param>
    /// <param name="contracts">Контракты для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task SetAsync(int count, IReadOnlyList<CourseContract> contracts, CancellationToken cancellationToken = default);
}
