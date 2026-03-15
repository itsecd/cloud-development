namespace ProjectApp.Domain.Entities;

/// <summary>
/// Кредитная заявка
/// </summary>
public class CreditApplication
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public required int Id { get; set; }
    /// <summary>
    /// Тип кредита
    /// </summary>
    public required string CreditType { get; set; }
    /// <summary>
    /// Запрашиваемая сумма
    /// </summary>
    public decimal RequestedAmount { get; set; }
    /// <summary>
    /// Срок в месяцах
    /// </summary>
    public int TermMonths { get; set; }
    /// <summary>
    /// Процентная ставка
    /// </summary>
    public double InterestRate { get; set; }
    /// <summary>
    /// Дата подачи
    /// </summary>
    public DateOnly ApplicationDate { get; set; }
    /// <summary>
    /// Необходимость страховки
    /// </summary>
    public bool RequiresInsurance { get; set; }
    /// <summary>
    /// Статус заявки
    /// </summary>
    public required string Status { get; set; }
    /// <summary>
    /// Дата решения
    /// </summary>
    public DateOnly? DecisionDate { get; set; }
    /// <summary>
    /// Одобренная сумма
    /// </summary>
    public decimal? ApprovedAmount { get; set; }
}
