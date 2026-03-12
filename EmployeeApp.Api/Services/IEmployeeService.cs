using EmployeeApp.Api.Entities;

namespace EmployeeApp.Api.Services;

/// <summary>
/// Сервис генерации данных сотрудников
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Получить сотрудника по идентификатору
    /// </summary>
    public Task<Employee> GetEmployeeById(int id);
}
