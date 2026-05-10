using Service.Api.Entities;

namespace Service.Api.Messaging;

/// <summary>
/// Публикует сведения о сотруднике в брокер сообщений.
/// </summary>
public interface IEmployeeEventPublisher
{
    /// <summary>
    /// Публикует событие генерации сотрудника.
    /// </summary>
    Task PublishAsync(Employee employee, CancellationToken cancellationToken = default);
}
