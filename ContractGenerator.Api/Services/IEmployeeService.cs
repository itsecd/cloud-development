using ContractGenerator.Shared.Models;

namespace ContractGenerator.Api.Services;

/// <summary>
/// Контракт сервиса, который возвращает сотрудника компании с учетом кэширования.
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Возвращает сотрудника по идентификатору. При отсутствии записи создает данные и помещает их в кэш.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника компании.</param>
    public Task<Employee> GetOrGenerateAsync(int id);
}
