using Cloud.Api.Models;

namespace Cloud.Api.Messaging;

/// <summary>
/// Интерфейс сервиса отправки сгенерированного сотрудника в очередь SQS
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Метод отправки сообщения в брокер
    /// </summary>
    /// <param name="employee">Информация о сотруднике</param>
    public Task SendMessage(Employee employee);
}
