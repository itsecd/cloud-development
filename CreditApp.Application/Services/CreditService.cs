using System.Text.Json;
using CreditApp.Application.Interfaces;
using CreditApp.Application.Options;
using CreditApp.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditApp.Application.Services;

public class CreditService(IDistributedCache distributedCache,
    ICreditApplicationGenerator generator,
    IOptions<CacheOptions> cacheOptions,
    ILogger<CreditService> logger)
    : ICreditService
{
    public async Task<CreditApplication> GetAsync(int id, CancellationToken ct)
    {
        var cacheKey = $"Credit_{id}";

        try
        {
            var cached = await distributedCache.GetStringAsync(cacheKey, ct);

            if (!string.IsNullOrEmpty(cached))
            {
                var deserialized = JsonSerializer.Deserialize<CreditApplication>(cached);
                if (deserialized is not null)
                {
                    logger.LogInformation("Cache HIT {CacheKey}", cacheKey);
                    return deserialized;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for {CacheKey}", cacheKey);
        }

        logger.LogInformation("Cache MISS {CacheKey}", cacheKey);

        var credit = await generator.GenerateAsync(id);

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheOptions.Value.AbsoluteExpirationMinutes)
            };

            await distributedCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(credit),
                options,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for {CacheKey}", cacheKey);
        }

        return credit;
    }
}
