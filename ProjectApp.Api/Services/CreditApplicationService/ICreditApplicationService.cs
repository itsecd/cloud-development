using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Сервис получения кредитной заявки
/// </summary>
public interface ICreditApplicationService
{
    public Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
