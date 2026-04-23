using Inventory.ApiService.Cache;
using Inventory.ApiService.Entity;
using Inventory.ApiService.Messaging;

namespace Inventory.ApiService.Services;

public class InventoryService(
    ILogger<InventoryService> logger,
    IInventoryCache cache,
    IProducerService producerService) : IInventoryService
{
    public async Task<Product> GetInventory(int id, CancellationToken cancellationToken = default)
    {
        var product = await cache.GetAsync(id, cancellationToken);

        await producerService.SendMessage(product);

        logger.LogInformation("Inventory {ResourceId} processed", id);

        return product;
    }
}