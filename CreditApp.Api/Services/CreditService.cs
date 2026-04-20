using CreditApp.Domain.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CreditApp.Api.Services;

/// <summary>
/// Служба для работы с кредитными заявками
/// </summary>
public class CreditService(
    IDistributedCache cache,
    ILogger<CreditService> logger,
    SqsProducer sqsProducer)
    : ICreditService
{
    private const string CachePrefix = "credit:";

    /// <summary>
    /// Возвращает кредитную заявку по идентификатору, используя кеш и публикуя событие в SQS при генерации
    /// </summary>
    public async Task<CreditApplication> GetAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var key = $"{CachePrefix}{id}";

        var cached = await cache.GetStringAsync(key, cancellationToken);

        if (cached is not null)
        {
            logger.LogInformation("Cache HIT for credit application {CreditId}", id);
            return JsonSerializer.Deserialize<CreditApplication>(cached)!;
        }

        logger.LogInformation("Cache MISS for credit application {CreditId}", id);

        var result = CreditGenerator.Generate(id);

        await sqsProducer.SendMessage(result);

        var serialized = JsonSerializer.Serialize(result);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        await cache.SetStringAsync(key, serialized, options, cancellationToken);

        logger.LogInformation(
            "Generated credit application {CreditId}. Type: {Type}, Amount: {Amount}, Status: {Status}",
            result.Id, result.CreditType, result.RequestedAmount, result.Status);

        return result;
    }
}
