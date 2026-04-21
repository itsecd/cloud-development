namespace ProjectApp.Domain.Entities;

/// <summary>
/// Модель кредитной заявки для API генерации и кэширования.
/// </summary>
public class CreditApplication
{
    /// <summary>Идентификатор заявки.</summary>
    public int Id { get; set; }
    /// <summary>Тип кредита.</summary>
    public string CreditType { get; set; } = string.Empty;
    /// <summary>Запрашиваемая сумма.</summary>
    public decimal RequestedAmount { get; set; }
    /// <summary>Срок кредита в месяцах.</summary>
    public int TermMonths { get; set; }
    /// <summary>Процентная ставка.</summary>
    public double InterestRate { get; set; }
    /// <summary>Дата подачи заявки.</summary>
    public DateOnly ApplicationDate { get; set; }
    /// <summary>Признак необходимости страховки.</summary>
    public bool RequiresInsurance { get; set; }
    /// <summary>Текущий статус заявки.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Дата принятия решения (для терминальных статусов).</summary>
    public DateOnly? DecisionDate { get; set; }
    /// <summary>Одобренная сумма (только для статуса "Одобрена").</summary>
    public decimal? ApprovedAmount { get; set; }
}
