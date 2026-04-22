using Inventory.ApiService.Entity;

namespace Inventory.ApiService.Messaging;

public interface IProducerService
{
    public Task SendMessage(Product product);
}