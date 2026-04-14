namespace Generator.Service.Cache;

/// <summary>
/// Интерфейс сервиса кэширования, абстрагирующий работу с распределённым кэшем (Redis).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получает объект из кэша по идентификатору.
    /// </summary>
    /// <typeparam name="T">Тип десериализуемого объекта.</typeparam>
    /// <param name="id">Идентификатор жилого здания (ключ кэша).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// Десериализованный объект типа <typeparamref name="T"/>, 
    /// или <c>default</c>, если объект отсутствует или произошла ошибка.
    /// </returns>
    public Task<T?> GetCache<T>(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Сохраняет объект в распределённый кэш с заданным временем жизни.
    /// </summary>
    /// <typeparam name="T">Тип сохраняемого объекта.</typeparam>
    /// <param name="id">Идентификатор жилого здания (ключ кэша).</param>
    /// <param name="obj">Объект для сохранения в кэше.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <c>true</c> — если объект успешно сохранён, 
    /// <c>false</c> — если произошла ошибка при записи.
    /// </returns>
    public Task<bool> SetCache<T>(int id, T obj, CancellationToken cancellationToken = default);
}