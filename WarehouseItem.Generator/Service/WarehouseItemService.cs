using WarehouseItem.Generator.DTO;
using WarehouseItem.Generator.Generator;

namespace WarehouseItem.Generator.Service;

/// <summary>
/// Реализация сервиса работы с товарами на складе.
/// </summary>
public sealed class WarehouseItemService(
    WarehouseItemGenerator generator,
    IWarehouseItemCache cache) : IWarehouseItemService
{
    /// <summary>
    /// Получить товар по идентификатору. Если товар не найден в кэше, генерирует новый и сохраняет в кэш.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO товара.</returns>
    public async Task<WarehouseItemDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync(id, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var generated = generator.Generate(id);
        await cache.SetAsync(id, generated, cancellationToken);

        return generated;
    }
}
