using Inventory.ApiService.Cache;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Messaging;

namespace Inventory.ApiService.Services;

/// <summary>
/// Сервис для обработки запросов, связанных с инвентарём
/// </summary>
/// <param name="logger">Сервис логирования операций инвентаря</param>
/// <param name="cache">Сервис кэширования данных о продуктах</param>
/// <param name="producerService">Сервис для отправки сообщений в брокер сообщений</param>
public class InventoryService(
    ILogger<InventoryService> logger,
    IInventoryCache cache,
    IProducerService producerService) : IInventoryService
{
    /// <summary>
    /// Получает информацию о продукте из кэша, отправляет сообщение о продукте в брокер сообщений
    /// и записывает информацию об обработке в лог
    /// </summary>
    /// <param name="id">Идентификатор продукта</param>
    /// <param name="cancellationToken">Токен для отмены асинхронной операции</param>
    /// <returns>Объект продукта, полученный из кэша</returns>
    public async Task<Product> GetInventory(int id, CancellationToken cancellationToken = default)
    {
        var product = await cache.GetAsync(id, cancellationToken);

        await producerService.SendMessage(product);

        logger.LogInformation("Inventory {ResourceId} processed", id);

        return product;
    }
}