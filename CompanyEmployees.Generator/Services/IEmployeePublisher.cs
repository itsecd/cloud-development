using CompanyEmployees.Generator.Models;

namespace CompanyEmployees.Generator.Services;

public interface IEmployeePublisher
{
    public Task PublishAsync(CompanyEmployeeModel employee, CancellationToken cancellationToken = default);
}