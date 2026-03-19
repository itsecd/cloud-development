using System;

namespace CreditSystem.Domain.Entities;

/// <summary>
/// Сущность кредитной заявки (Вариант 30)
/// </summary>
public class LoanApplication
{
    /// <summary>
    /// Уникальный номер заявки
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ФИО заемщика
    /// </summary>
    public required string ApplicantName { get; set; }

    /// <summary>
    /// Сумма кредита (в рублях)
    /// </summary>
    public decimal RequestedAmount { get; set; }

    /// <summary>
    /// Срок кредитования (в месяцах)
    /// </summary>
    public int TermMonths { get; set; }

    /// <summary>
    /// Ежемесячный доход
    /// </summary>
    public decimal MonthlyIncome { get; set; }

    /// <summary>
    /// Кредитный рейтинг (0-1000)
    /// </summary>
    public int CreditScore { get; set; }

    /// <summary>
    /// Текущий статус заявки
    /// </summary>
    public string Status { get; set; } = "New";

    /// <summary>
    /// Дата и время подачи
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
