using Service.Api.Entities;

namespace Service.Api.Messaging;

/// <summary>
/// Интерфейс службы для отправки генерируемых сотрудников в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Отправляет сообщение в брокер
    /// </summary>
    /// <param name="employee">Сотрудник</param>
    public Task SendMessage(Employee employee);
}
