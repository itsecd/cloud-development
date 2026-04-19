using WarehouseApp.Api.Models;

namespace WarehouseApp.Api.Services;

/// <summary>
/// Сервис получения товара на складе с кэшированием
/// </summary>
public interface IWarehouseItemService
{
    /// <summary>
    /// Возвращает товар по идентификатору: из кэша или генерирует новый
    /// </summary>
    /// <param name="id">Идентификатор товара в системе</param>
    /// <returns>Сгенерированный товар на складе</returns>
    public Task<WarehouseItem> GetOrGenerate(int id);
}
