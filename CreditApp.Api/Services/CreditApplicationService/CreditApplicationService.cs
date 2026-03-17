using CreditApp.Domain.Entities;

namespace CreditApp.Api.Services.CreditApplicationService;

/// <summary>
/// Основной сервис управления кредитными заявками.
/// Координирует работу с кэшем, хранилищем и генератором заявок.
/// </summary>
public class CreditApplicationService(
    CreditApplicationCacheService cacheService,
    CreditApplicationStorageService storageService,
    CreditApplicationGenerator generator,
    ILogger<CreditApplicationService> logger)
{
    public async Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedApplication = await cacheService.GetAsync(id, cancellationToken);
            if (cachedApplication != null)
            {
                return cachedApplication;
            }

            logger.LogInformation("Заявка {Id} не найдена в кэше, проверяем MinIO", id);

            var storedApplication = await storageService.GetAsync(id, cancellationToken);
            if (storedApplication != null)
            {
                logger.LogInformation("Заявка {Id} найдена в MinIO, кэшируем", id);
                await cacheService.SetAsync(storedApplication, cancellationToken);
                return storedApplication;
            }

            logger.LogInformation("Заявка {Id} не найдена в хранилище, генерируем новую", id);

            var newApplication = generator.Generate(id);
            await cacheService.SetNewAsync(newApplication, cancellationToken);

            logger.LogInformation(
                "Кредитная заявка сгенерирована и закэширована: Id={Id}, Тип={Type}, Сумма={Amount}, Статус={Status}",
                newApplication.Id,
                newApplication.Type,
                newApplication.Amount,
                newApplication.Status);

            return newApplication;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении/генерации заявки {Id}", id);
            throw;
        }
    }
}
