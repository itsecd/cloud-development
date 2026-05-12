using ProjectApp.Domain.Entities;

namespace ProjectApp.Domain.Messaging;

/// <summary>
/// Событие о сгенерированной кредитной заявке, отправляемое в брокер сообщений.
/// </summary>
public class CreditApplicationGeneratedEvent
{
    /// <summary>
    /// Идентификатор заявки.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Время создания события (UTC).
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Данные заявки.
    /// </summary>
    public CreditApplication Application { get; set; } = new();
}
