using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Контракт сервиса получения кредитной заявки.
/// </summary>
public interface ICreditApplicationService
{
    /// <summary>
    /// Возвращает заявку по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заявки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Кредитная заявка.</returns>
    Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
