using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Сервис получения кредитной заявки с кэшированием в Redis через IDistributedCache.
/// </summary>
public class CreditApplicationService(
    IDistributedCache cache,
    CreditApplicationGenerator generator,
    ICreditApplicationEventPublisher eventPublisher,
    IConfiguration configuration,
    ILogger<CreditApplicationService> logger) : ICreditApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(
        Math.Max(1, configuration.GetValue<int?>("CacheSettings:ExpirationMinutes") ?? 10));

    /// <summary>
    /// Возвращает кредитную заявку по идентификатору.
    /// Если запись есть в кэше, возвращается она; иначе генерируется новая и сохраняется в кэш.
    /// </summary>
    /// <param name="id">Идентификатор заявки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Кредитная заявка.</returns>
    public async Task<CreditApplication> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(id);

        logger.LogInformation("Looking up credit application {Id} in Redis cache", id);
        try
        {
            var cachedPayload = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedPayload))
            {
                var cachedApplication = JsonSerializer.Deserialize<CreditApplication>(cachedPayload, JsonOptions);
                if (cachedApplication is not null)
                {
                    logger.LogInformation("Cache hit for credit application {Id}", id);
                    return cachedApplication;
                }

                logger.LogWarning("Cache entry for credit application {Id} cannot be deserialized. Regenerating value.", id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read credit application {Id} from cache", id);
        }

        logger.LogInformation("Cache miss for credit application {Id}. Generating new value.", id);
        var generatedApplication = generator.Generate();
        generatedApplication.Id = id;

        var payload = JsonSerializer.Serialize(generatedApplication, JsonOptions);
        try
        {
            await cache.SetStringAsync(
                cacheKey,
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheTtl
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write credit application {Id} to cache", id);
        }

        logger.LogInformation(
            "Generated and cached credit application {Id}: CreditType={CreditType}, RequestedAmount={RequestedAmount}, Status={Status}",
            generatedApplication.Id,
            generatedApplication.CreditType,
            generatedApplication.RequestedAmount,
            generatedApplication.Status);

        try
        {
            await eventPublisher.PublishGeneratedAsync(generatedApplication, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish generated credit application event for Id={Id}", generatedApplication.Id);
        }

        return generatedApplication;
    }

    private static string BuildCacheKey(int id) => $"credit-application:{id}";
}
