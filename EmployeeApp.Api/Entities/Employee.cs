namespace EmployeeApp.Api.Entities;

/// <summary>
/// Сотрудник компании
/// </summary>
public class Employee
{
    /// <summary>
    /// Идентификатор сотрудника в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ФИО
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Должность
    /// </summary>
    public required string Position { get; set; }

    /// <summary>
    /// Отдел
    /// </summary>
    public required string Department { get; set; }

    /// <summary>
    /// Дата приема
    /// </summary>
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// Оклад
    /// </summary>
    public decimal Salary { get; set; }

    /// <summary>
    /// Электронная почта
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Номер телефона
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Индикатор увольнения
    /// </summary>
    public bool IsDismissed { get; set; }

    /// <summary>
    /// Дата увольнения
    /// </summary>
    public DateOnly? DismissalDate { get; set; }
}
