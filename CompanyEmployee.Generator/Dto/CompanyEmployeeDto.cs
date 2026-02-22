namespace CompanyEmployee.Generator.Dto;

/// <summary>
/// Информация о сотруднике компании
/// </summary>
public class CompanyEmployeeDto
{
    /// <summary>
    /// Идентификатор сотрудника в системе
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// ФИО
    /// </summary>
    public string FullName { get; set; }
    
    /// <summary>
    /// Должность
    /// </summary>
    public string Position { get; set; }
    
    /// <summary>
    /// Отдел
    /// </summary>
    public string Department { get; set; }
    
    /// <summary>
    /// Дата приема
    /// </summary>
    public DateOnly EmploymentDate { get; set; }
    
    /// <summary>
    /// Оклад
    /// </summary>
    public decimal Salary { get; set; }
    
    /// <summary>
    /// Электронная почта
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Номер телефона
    /// </summary>
    public string PhoneNumber { get; set; }
    
    /// <summary>
    /// Индикатор увольнения
    /// </summary>
    public bool DismissalFlag { get; set; }
    
    /// <summary>
    /// Дата увольнения
    /// </summary>
    public DateOnly? DismissalDate { get; set; }
}