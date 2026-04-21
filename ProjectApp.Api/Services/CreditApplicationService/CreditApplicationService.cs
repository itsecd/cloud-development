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
    CreditApplicationValidator validator,
    ILogger<CreditApplicationService> logger) : ICreditApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

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
        var cachedPayload = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedPayload))
        {
            var cachedApplication = JsonSerializer.Deserialize<CreditApplication>(cachedPayload, JsonOptions);
            if (cachedApplication is not null && validator.TryValidate(cachedApplication, out _))
            {
                logger.LogInformation("Cache hit for credit application {Id}", id);
                return cachedApplication;
            }

            if (cachedApplication is null)
            {
                logger.LogWarning("Cache entry for credit application {Id} cannot be deserialized. Regenerating value.", id);
            }
            else
            {
                validator.TryValidate(cachedApplication, out var cacheValidationError);
                logger.LogWarning(
                    "Cache entry for credit application {Id} is invalid: {ValidationError}. Regenerating value.",
                    id,
                    cacheValidationError);
            }
        }

        logger.LogInformation("Cache miss for credit application {Id}. Generating new value.", id);
        var generatedApplication = generator.Generate();
        generatedApplication.Id = id;

        if (!validator.TryValidate(generatedApplication, out var generatedValidationError))
        {
            throw new InvalidOperationException($"Generated application is invalid: {generatedValidationError}");
        }

        var payload = JsonSerializer.Serialize(generatedApplication, JsonOptions);
        await cache.SetStringAsync(
            cacheKey,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            },
            cancellationToken);

        logger.LogInformation(
            "Generated and cached credit application {Id}: CreditType={CreditType}, RequestedAmount={RequestedAmount}, Status={Status}",
            generatedApplication.Id,
            generatedApplication.CreditType,
            generatedApplication.RequestedAmount,
            generatedApplication.Status);

        return generatedApplication;
    }

    private static string BuildCacheKey(int id) => $"credit-application:{id}";
}
