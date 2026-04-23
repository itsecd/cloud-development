using Inventory.ApiService.Entity;

namespace Inventory.ApiService.Services;

public interface IInventoryService
{
    public Task<Product> GetInventory(int id, CancellationToken cancellationToken = default);
}