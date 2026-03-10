namespace Employee.ApiService.Models;

/// <summary>
/// Класс сотрудник компании
/// </summary>
public class EmployeeModel
{

    /// <summary>
    /// Идентификатор сотрудника в системе
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// ФИО
    /// </summary>
    public required string Name { get; set; }

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
    public required DateOnly DateAdmission { get; set; }

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
    public required string Phone { get; set; }

    /// <summary>
    /// Индикатор увольнения
    /// </summary>
    public bool DismissalIndicator { get; set; } = false;

    /// <summary>
    /// Дата увольнения
    /// </summary>
    public DateOnly? DateDismissal { get; set; }
}
