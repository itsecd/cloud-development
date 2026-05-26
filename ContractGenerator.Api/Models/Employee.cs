namespace ContractGenerator.Api.Models;

/// <summary>
/// Информация о сотруднике компании.
/// </summary>
public class Employee
{
    /// <summary>
    /// Идентификатор сотрудника в системе.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// ФИО сотрудника.
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Должность сотрудника.
    /// </summary>
    public required string Position { get; set; }

    /// <summary>
    /// Отдел, в котором работает сотрудник.
    /// </summary>
    public required string Department { get; set; }

    /// <summary>
    /// Дата приема на работу.
    /// </summary>
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// Зарплата сотрудника.
    /// </summary>
    public required decimal Salary { get; set; }

    /// <summary>
    /// Электронная почта сотрудника.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Номер телефона сотрудника.
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Признак увольнения сотрудника.
    /// </summary>
    public required bool IsFired { get; set; }

    /// <summary>
    /// Дата увольнения сотрудника.
    /// </summary>
    public DateOnly? FiredDate { get; set; }
}
