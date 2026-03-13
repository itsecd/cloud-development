using System.Threading;
using System.Threading.Tasks;
using WarehouseItem.Generator.DTO;

namespace WarehouseItem.Generator.Service;

/// <summary>
/// Интерфейс для кэширования товаров.
/// </summary>
public interface IWarehouseItemCache
{
    /// <summary>
    /// Получить товар из кэша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO товара или null, если не найден в кэше.</returns>
    public Task<WarehouseItemDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить товар в кэш.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="value">DTO товара для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public Task SetAsync(int id, WarehouseItemDto value, CancellationToken cancellationToken = default);
}