using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationGeneratorService;

/// <summary>
/// Сервис получения кредитной заявки
/// </summary>
public interface ICreditApplicationGeneratorService
{
    public Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
