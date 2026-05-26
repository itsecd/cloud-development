using ContractGenerator.Api.Models;

namespace ContractGenerator.Api.Services;

/// <summary>
/// Контракт генератора данных сотрудника компании.
/// </summary>
public interface IEmployeeGenerator
{
    /// <summary>
    /// Создает сотрудника компании для переданного идентификатора.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника компании.</param>
    public Employee Generate(int id);
}
