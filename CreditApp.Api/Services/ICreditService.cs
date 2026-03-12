using CreditApp.Domain.Data;

namespace CreditApp.Api.Services;

/// <summary>
/// Интерфейс сервиса для работы с кредитными заявками
/// </summary>
public interface ICreditService
{
    /// <summary>
    /// Получить кредитную заявку по идентификатору
    /// </summary>
    public Task<CreditApplication> GetAsync(
        int id,
        CancellationToken cancellationToken = default);
}