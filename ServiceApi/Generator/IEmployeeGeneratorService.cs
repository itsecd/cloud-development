using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Интерфейс для записи юзкейса по обработке сотрудников компании
/// </summary>
public interface IEmployeeGeneratorService
{
    Task<Employee> ProcessEmployee(int id);

}
