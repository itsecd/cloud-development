using Cloud.API.Models;

namespace Cloud.API.Services;

/// <summary>
/// Интерфейс сервис для получения сотрудника компании по id с кэшированием
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Получпет сотрудника компании по id. Если сотрудник не найден, 
    /// то создает нового с данным id и сохраняет его в кэше
    /// </summary>
    /// <param name="id">Идентификатор сотрудника компании</param>
    /// <returns></returns>
    public Task<Employee> GetOrGenerateAsync(int id);
}
