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
    /// ФИО клиента
    /// </summary>
    public required string ClientFullName { get; set; }
    
    /// <summary>
    /// Дата подачи заявки
    /// </summary>
    public DateOnly ApplicationDate { get; set; }
    
    /// <summary>
    /// Сумма кредита
    /// </summary>
    public decimal CreditAmount { get; set; }
    
    /// <summary>
    /// Срок кредита в месяцах
    /// </summary>
    public int CreditTermMonths { get; set; }
    
    /// <summary>
    /// Цель кредита
    /// </summary>
    public required string CreditPurpose { get; set; }
    
    /// <summary>
    /// Доход клиента
    /// </summary>
    public decimal ClientIncome { get; set; }
    
    /// <summary>
    /// Кредитный рейтинг (0-1000)
    /// </summary>
    public int CreditScore { get; set; }
    
    /// <summary>
    /// Дата принятия решения
    /// </summary>
    public DateOnly? DecisionDate { get; set; }
    
    /// <summary>
    /// Решение по заявке: одобрено (true), отклонено (false), null - ещё не решено
    /// </summary>
    public bool? Approved { get; set; }
    
    /// <summary>
    /// Процентная ставка (годовых)
    /// </summary>
    public decimal InterestRate { get; set; }
    
    /// <summary>
    /// Ежемесячный платёж
    /// </summary>
    public decimal MonthlyPayment { get; set; }
}
