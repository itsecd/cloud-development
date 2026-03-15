using Microsoft.Extensions.Caching.Distributed;
using ProjectApp.Domain.Entities;
using System.Text.Json;

namespace ProjectApp.Api.Services.CreditApplicationGeneratorService;

/// <summary>
/// Сервис получения кредитной заявки: сначала ищет в кэше, при промахе — генерирует новую и сохраняет
/// </summary>
public class CreditApplicationGeneratorService(
    IDistributedCache cache,
    CreditApplicationGenerator generator,
    IConfiguration configuration,
    ILogger<CreditApplicationGeneratorService> logger) : ICreditApplicationGeneratorService
{
    private readonly int _expirationMinutes = configuration.GetValue("CacheSettings:ExpirationMinutes", 10);

    /// <summary>
    /// Возвращает кредитную заявку по идентификатору.
    /// Если найдена в кэше — возвращается из него; иначе генерируется, сохраняется в кэш и возвращается.
    /// </summary>
    public async Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to retrieve credit application {Id} from cache", id);

        var cacheKey = $"credit-application-{id}";

        CreditApplication? application = null;
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                application = JsonSerializer.Deserialize<CreditApplication>(cachedData);
                if (application != null)
                {
                    logger.LogInformation("Credit application {Id} found in cache", id);
                    return application;
                }
                logger.LogWarning("Credit application {Id} found in cache but could not be deserialized. Generating a new one", id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve credit application {Id} from cache (error ignored)", id);
        }

        logger.LogInformation("Credit application {Id} not found in cache or cache unavailable, generating a new one", id);
        application = generator.Generate();
        application.Id = id;

        try
        {
            logger.LogInformation("Saving credit application {Id} to cache", id);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expirationMinutes)
            };

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(application),
                cacheOptions,
                cancellationToken);

            logger.LogInformation(
                "Credit application generated and cached: Id={Id}, Type={CreditType}, Requested={RequestedAmount}, Status={Status}, Approved={ApprovedAmount}",
                application.Id,
                application.CreditType,
                application.RequestedAmount,
                application.Status,
                application.ApprovedAmount);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save credit application {Id} to cache (error ignored)", id);
        }

        return application;
    }
}
