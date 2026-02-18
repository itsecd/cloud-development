using CreditApp.Domain.Entities;

namespace CreditApp.Application.Interfaces;

/// <summary>
/// Сервис для работы с объектами кредитных заявок.
/// Предоставляет операции получения и управления заявками в приложении.
/// </summary>
public interface ICreditService
{
    /// <summary>
    /// Асинхронно получает заявку на кредит по её идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заявки.</param>
    /// <param name="ct">Токен отмены для асинхронной операции.</param>
    /// <returns>Экземпляр <see cref="CreditApplication"/>, соответствующий запрошенному идентификатору.</returns>
    public Task<CreditApplication> GetAsync(int id, CancellationToken ct);

}
