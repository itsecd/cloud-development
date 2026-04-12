namespace CourseManagement.ApiService.Cache;

/// <summary>
/// Универсальный интерфейс для работы с кэшем
/// </summary>
public interface ICacheService<T>
{
    /// <summary>
    /// Асинхронный метод для извлечения данных из кэша
    /// </summary>
    /// <param name="key">Ключ для сущности</param>
    /// <param name="id">Идентификатор сущности</param>
    /// <returns>Объект или null при его отсутствии</returns>
    public Task<T?> FetchAsync(string key, int id);

    /// <summary>
    /// Асинхронный метод для внесения данных в кэш
    /// </summary>
    /// <param name="key">Ключ для сущности</param>
    /// <param name="id">Идентификатор сущности</param>
    /// <param name="entity">Кешируемая сущность</param>
    public Task StoreAsync(string key, int id, T entity);
}
