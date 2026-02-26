using CompanyEmployee.Generator.Dto;

namespace CompanyEmployee.Generator.Service;

/// <summary>
/// Интерфейс сервиса получения сотрудника компании
/// </summary>
public interface ICompanyEmployeeService
{
    /// <summary>
    /// Метод получения сотрудника компании по идентификатору
    /// Сначала пытается найти сотрудника в кэше, если не находит, то генерирует нового и записывает его в кэш
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника</param>
    /// <param name="token">Токен отмены запроса</param>
    /// <returns>DTO сотрудника компании</returns>
    public Task<CompanyEmployeeDto> GetByIdAsync(int employeeId, CancellationToken token);
}