using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

public interface ICreditApplicationService
{
    Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
