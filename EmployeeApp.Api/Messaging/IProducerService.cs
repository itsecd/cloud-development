using EmployeeApp.Api.Entities;

namespace EmployeeApp.Api.Messaging;

/// <summary>
/// Интерфейс службы для отправки сгенерированных сотрудников в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Отправляет сообщение в брокер
    /// </summary>
    /// <param name="employee">Сотрудник</param>
    public Task SendMessage(Employee employee);
}
