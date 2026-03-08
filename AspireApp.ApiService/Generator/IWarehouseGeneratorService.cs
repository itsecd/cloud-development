using AspireApp.ApiService.Entities;

namespace AspireApp.ApiService.Generator;

/// <summary>
/// Сервис обработки товаров на складе
/// </summary>
public interface IWarehouseGeneratorService
{
    Task<Warehouse> ProcessWarehouse(int id);
}