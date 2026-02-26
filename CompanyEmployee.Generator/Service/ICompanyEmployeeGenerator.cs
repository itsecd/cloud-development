using CompanyEmployee.Generator.Dto;

namespace CompanyEmployee.Generator.Service;

/// <summary>
/// Интерфейс генератора сотрудника по идентификатору
/// </summary>
public interface ICompanyEmployeeGenerator
{
    /// <summary>
    /// Метод для генерации сотрудника по идентификатору
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника</param>
    /// <returns>DTO сотрудника компании</returns>
    public CompanyEmployeeDto Generate(int employeeId);

}