namespace CompanyEmployee.ApiService.Models;

/// <summary>
/// Модель сотрудника компании
/// </summary>
public class CompanyEmployeeModel
{
    /// <summary>
    /// Идентификатор сотрудника в системе
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// ФИО
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Должность
    /// </summary>
    public required string JobTitle { get; set; }

    /// <summary>
    /// Отдел
    /// </summary>
    public required string Department { get; set; }

    /// <summary>
    /// Дата приема
    /// </summary>
    public required DateOnly AdmissionDate { get; set; }

    /// <summary>
    /// Оклад
    /// </summary>
    public required decimal Salary { get; set; }

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
    public bool Dismissal { get; set; } = false;

    /// <summary>
    /// Дата увольнения
    /// </summary>
    public DateOnly? DismissalDate { get; set; }
}
