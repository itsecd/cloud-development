using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Публикатор событий о сгенерированных кредитных заявках.
/// </summary>
public interface ICreditApplicationEventPublisher
{
    /// <summary>
    /// Публикует событие о сгенерированной заявке.
    /// </summary>
    /// <param name="application">Сгенерированная заявка.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task PublishGeneratedAsync(CreditApplication application, CancellationToken cancellationToken);
}
