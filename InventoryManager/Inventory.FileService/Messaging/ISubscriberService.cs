namespace Inventory.FileService.Messaging;

/// <summary>
/// Интерфейс службы подписки на сообщения
/// </summary>
public interface ISubscriberService
{
    /// <summary>
    /// Выполняет инициализацию подписки при старте приложения
    /// </summary>
    public Task SubscribeEndpoint();
}