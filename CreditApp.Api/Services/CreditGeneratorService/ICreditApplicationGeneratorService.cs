using CreditApp.Domain.Entities;

namespace CreditApp.Api.Services.CreditGeneratorService;

public interface ICreditApplicationGeneratorService
{
    /// <summary>
    /// Получить заявку по ID, если не найдена в кэше генерируем новую с указанным ID
    /// </summary>
    /// <param name="id">ID кредитной заявки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Кредитная заявка</returns>
    public Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
