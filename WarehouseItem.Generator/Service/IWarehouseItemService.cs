using WarehouseItem.Generator.DTO;

namespace WarehouseItem.Generator.Service;

/// <summary>
/// Интерфейс для сервиса работы с товарами на складе.
/// </summary>
public interface IWarehouseItemService
{
    /// <summary>
    /// Получить товар по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO товара.</returns>
    public Task<WarehouseItemDto> GetAsync(int id, CancellationToken cancellationToken = default);
}
