using ContractGenerator.Shared.Models;

namespace ContractGenerator.Api.Services;

/// <summary>
/// Сервис публикации сгенерированных сотрудников в брокер сообщений.
/// </summary>
public interface IEmployeePublisher
{
    /// <summary>
    /// Публикует данные сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник компании.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public Task PublishAsync(Employee employee, CancellationToken cancellationToken = default);
}
