using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CreditSystem.Domain.Entities;
using CreditSystem.Api.Services;

namespace CreditSystem.Api.Services;

/// <summary>
/// Сервис управления кредитными заявками (с кэшированием)
/// </summary>
public class LoanService(
    IDistributedCache cache,
    LoanDataGenerator generator,
    ILogger<LoanService> logger)
{
    private const int CacheExpirationMinutes = 15;

    public async Task<LoanApplication> GetApplicationAsync(Guid id, CancellationToken ct = default)
    {
        var key = $"loan:app:{id}";
        
        // Пытаемся взять из редиса
        var data = await cache.GetStringAsync(key, ct);
        if (!string.IsNullOrEmpty(data))
        {
            logger.LogInformation("Заявка {Id} найдена в кэше", id);
            return JsonSerializer.Deserialize<LoanApplication>(data)!;
        }

        // Если нет — генерим
        logger.LogWarning("Заявка {Id} не найдена. Генерируем новую...", id);
        var app = generator.Generate();
        app.Id = id;

        // И кладем обратно
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
        };

        await cache.SetStringAsync(key, JsonSerializer.Serialize(app), options, ct);
        
        return app;
    }
}
