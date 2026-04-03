namespace CreditApp.Messaging.Contracts;

/// <summary>
/// Событие, возникающее при генерации новой кредитной заявки.
/// Используется для обмена данными между сервисами через брокер сообщений SQS.
/// </summary>
public class CreditGeneratedEvent
{
    public int Id { get; set; }

    public string CreditType { get; set; } = string.Empty;

    public decimal RequestedAmount { get; set; }

    public int TermMonths { get; set; }

    public double InterestRate { get; set; }

    public DateOnly ApplicationDate { get; set; }

    public bool HasInsurance { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateOnly? DecisionDate { get; set; }

    public decimal? ApprovedAmount { get; set; }

    public DateTime GeneratedAt { get; set; }
}