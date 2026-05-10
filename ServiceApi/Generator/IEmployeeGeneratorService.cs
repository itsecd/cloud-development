using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Интерфейс обработки запросов на получение сотрудника.
/// </summary>
public interface IEmployeeGeneratorService
{
    Task<Employee> ProcessEmployee(int id, CancellationToken cancellationToken = default);
}
