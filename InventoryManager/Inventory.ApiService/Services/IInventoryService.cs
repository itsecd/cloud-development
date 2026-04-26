using Inventory.ApiService.Entity;

namespace Inventory.ApiService.Services;

/// <summary>
/// Интерфейс сервиса для работы с инвентарём
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Получает информацию о продукте по его идентификатору
    /// </summary>
    /// <param name="id"> Идентификатор продукта</param>
    /// <param name="cancellationToken"> Токен для отмены асинхронной операции</param>
    /// <returns> Объект продукта</returns>
    public Task<Product> GetInventory(int id, CancellationToken cancellationToken = default);
}