using System.Text.Json;
using CreditApp.Application.Interfaces;
using CreditApp.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CreditApp.Application.Services;

public class CreditService(IDistributedCache distributedCache,
    ICreditApplicationGenerator generator,
    ILogger<CreditService> logger)
    : ICreditService
{
    public async Task<CreditApplication> GetAsync(int id, CancellationToken ct)
    {
        var cacheKey = $"Credit_{id}";

        var cached = await distributedCache.GetStringAsync(cacheKey, ct);

        if (!string.IsNullOrEmpty(cached))
        {
            logger.LogInformation("Cache HIT {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<CreditApplication>(cached)!;
        }

        logger.LogInformation("Cache MISS {CacheKey}", cacheKey);

        var credit = await generator.GenerateAsync(id, ct);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(credit),
            options,
            ct);

        return credit;
    }
}
