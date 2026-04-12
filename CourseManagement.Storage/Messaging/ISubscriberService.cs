namespace CourseManagement.Storage.Messaging;

/// <summary>
/// Интерфейс сервиса для подписки на топик
/// </summary>
public interface ISubscriberService
{
    /// <summary>
    /// Метод для подписки на топик
    /// </summary>
    /// <returns>Успешность операции подписки</returns>
    public Task<bool> SubscribeEndpoint();
}
