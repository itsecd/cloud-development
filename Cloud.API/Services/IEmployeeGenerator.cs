using Cloud.API.Models;

namespace Cloud.API.Services;

/// <summary>
/// Интерфейс генератора сотрудника компании по id
/// </summary>
public interface IEmployeeGenerator
{
    /// <summary>
    /// Генерирует сотрудника компании с указанным id
    /// </summary>
    /// <param name="id">Идентификатор сотрудника компании</param>
    public Employee Generate(int id);
}