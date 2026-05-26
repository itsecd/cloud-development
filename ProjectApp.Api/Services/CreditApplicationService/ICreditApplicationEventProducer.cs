using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Producer событий о сгенерированных кредитных заявках.
/// </summary>
public interface ICreditApplicationEventProducer
{
    /// <summary>
    /// Отправляет событие о сгенерированной заявке.
    /// </summary>
    /// <param name="application">Сгенерированная заявка.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task ProduceGeneratedAsync(CreditApplication application, CancellationToken cancellationToken);
}
