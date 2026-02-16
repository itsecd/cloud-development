namespace CreditApp.Domain.Entities;

/// <summary>
/// Кредитная заявка
/// </summary>
public class CreditApplication
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Тип кредита
    /// </summary>
    public string Type { get; set; } = String.Empty;
    /// <summary>
    /// Запрашиваемая сумма
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Срок в месяцах
    /// </summary>
    public int Term { get; set; }
    /// <summary>
    /// Процентная ставка
    /// </summary>
    public double InterestRate { get; set; }
    /// <summary>
    /// Дата подачи
    /// </summary>
    public DateOnly SubmissionDate { get; set; }
    /// <summary>
    /// Необходимость страховки
    /// </summary>
    public bool RequiresInsurance { get; set; }
    /// <summary>
    /// Статус заявки
    /// </summary>
    public string Status { get; set; } = String.Empty;
    /// <summary>
    /// Дата решения
    /// </summary>
    public DateOnly? ApprovalDate { get; set; }
    /// <summary>
    /// Одобренная сумма
    /// </summary>
    public decimal? ApprovedAmount { get; set; }
}
