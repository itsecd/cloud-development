namespace CourseManagement.ApiService.Messaging;

/// <summary>
/// Универсальный интерфейс сервиса для отправки генерируемых сущностей в брокер сообщений
/// </summary>
public interface IPublisherService<T>
{
    /// <summary>
    /// Метод для отправки сообщения в брокер
    /// </summary>
    /// <param name="id">Идентификатор отправляемой сущности</param>
    /// <param name="entity">Отправляемая сущность</param>
    /// <param name="cancellationToken">Токен для возможности отмены ожидания после отправки</param>
    /// <returns>Успешность операции отправки</returns>
    public Task<bool> SendMessage(int id, T entity, CancellationToken cancellationToken = default);
}
