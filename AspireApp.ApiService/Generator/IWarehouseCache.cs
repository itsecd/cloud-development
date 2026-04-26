using AspireApp.ApiService.Entities;

namespace AspireApp.ApiService.Generator;

/// <summary>
/// Интерфейс для работы с кэшем товаров
/// </summary>
public interface IWarehouseCache
{
    /// <summary>
    /// Получить товар из кэша по идентификатору
    /// </summary>
    Task<Warehouse?> GetAsync(int id);

    /// <summary>
    /// Сохранить товар в кэш
    /// </summary>
    Task SetAsync(Warehouse warehouse);
}