using Inventory.ApiService.Entity;

namespace Inventory.ApiService.Cache;

/// <summary>
/// Интерфейс сервиса для получения продукта с использованием кэширования.
/// </summary>
public interface IInventoryCache
{
    /// <summary>
    /// Возвращает продукт по идентификатору из кэша или генерирует его при отсутствии в кэше.
    /// </summary>
    /// <param name="id"> Идентификатор продукта</param>
    /// <param name="ct"> Токен отмены операции</param>
    /// <returns> Экземпляр продукта</returns>
    public Task<Product> GetAsync(int id, CancellationToken ct);
}