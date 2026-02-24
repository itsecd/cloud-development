namespace CompanyEmployee.Generator.Dto;

/// <summary>
/// Информация о сотруднике компании
/// </summary>
public class CompanyEmployeeDto
{
    /// <summary>
    /// Идентификатор сотрудника в системе
    /// </summary>
    public required int Id { get; init; }
    
    /// <summary>
    /// ФИО
    /// </summary>
    public required string FullName { get; init; }
    
    /// <summary>
    /// Должность
    /// </summary>
    public required string Position { get; init; }
    
    /// <summary>
    /// Отдел
    /// </summary>
    public required string Department { get; init; }
    
    /// <summary>
    /// Дата приема
    /// </summary>
    public required DateOnly EmploymentDate { get; init; }
    
    /// <summary>
    /// Оклад
    /// </summary>
    public required decimal Salary { get; init; }
    
    /// <summary>
    /// Электронная почта
    /// </summary>
    public required string Email { get; init; }
    
    /// <summary>
    /// Номер телефона
    /// </summary>
    public required string PhoneNumber { get; init; }
    
    /// <summary>
    /// Индикатор увольнения
    /// </summary>
    public required bool DismissalFlag { get; init; }
    
    /// <summary>
    /// Дата увольнения
    /// </summary>
    public DateOnly? DismissalDate { get; init; }
}