namespace CreditApp.Domain.Data;

/// <summary>
/// Класс, представляющий кредитную заявку
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
    public string CreditType { get; set; } = default!;
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
    public bool HasInsurance { get; set; }
    /// <summary>
    /// Статус заявки
    /// </summary>
    public string Status { get; set; } = default!;
    /// <summary>
    /// Дата решения
    /// </summary>
    public DateOnly? DecisionDate { get; set; }
    /// <summary>
    /// Одобренная сумма
    /// </summary>
    public decimal? ApprovedAmount { get; set; }
}
