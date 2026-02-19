namespace Generator.Dto;

/// <summary>
/// DTO кредитной заявки, возвращаемый HTTP API.
/// </summary>
public class CreditOrderDto
{
    /// <summary>Идентификатор заявки.</summary>
    public int Id { get; set; }

    /// <summary>Тип кредита (например: Потребительский, Ипотека).</summary>
    public string CreditType { get; set; } = "";

    /// <summary>Запрошенная сумма кредита.</summary>
    public decimal RequestedSum { get; set; }

    /// <summary>Срок кредита в месяцах.</summary>
    public int MonthsDuration { get; set; }

    /// <summary>Процентная ставка (годовых).</summary>
    public double InterestRate { get; set; }

    /// <summary>Дата подачи заявки.</summary>
    public DateOnly FilingDate { get; set; }

    /// <summary>Признак необходимости страховки.</summary>
    public bool IsInsuranceNeeded { get; set; }

    /// <summary>Статус заявки: Новая / В обработке / Одобрена / Отклонена.</summary>
    public string OrderStatus { get; set; } = "";

    /// <summary>
    /// Дата принятия решения. Заполняется только для конечных статусов (например, Одобрена/Отклонена).
    /// </summary>
    public DateOnly? DecisionDate { get; set; }

    /// <summary>
    /// Одобренная сумма. Заполняется только при статусе "Одобрена".
    /// </summary>
    public decimal? ApprovedSum { get; set; }
}
