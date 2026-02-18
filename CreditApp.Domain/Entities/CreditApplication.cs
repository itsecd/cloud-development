namespace CreditApp.Domain.Entities;

/// <summary>
/// Кредитная заявка
/// </summary>
public class CreditApplication
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; init; }
    /// <summary>
    /// Тип кредита
    /// </summary>
    public required string CreditType { get; init; }
    /// <summary>
    /// Запрашиваемая сумма
    /// </summary>
    public decimal RequestedAmount { get; init; }
    /// <summary>
    /// Срок в месяцах
    /// </summary>
    public int TermMonths { get; init; }
    /// <summary>
    /// Процентная ставка
    /// </summary>
    public double InterestRate { get; init; }
    /// <summary>
    /// Дата подачи
    /// </summary>
    public DateOnly SubmissionDate { get; init; }
    /// <summary>
    /// Необходимость страховки
    /// </summary>
    public bool HasInsurance { get; init; }
    /// <summary>
    /// Статус заявки
    /// </summary>
    public required string Status { get; init; }
    /// <summary>
    /// Дата решения
    /// </summary>
    public DateOnly? DecisionDate { get; init; }
    /// <summary>
    /// Одобренная сумма
    /// </summary>
    public decimal? ApprovedAmount { get; init; }
}
