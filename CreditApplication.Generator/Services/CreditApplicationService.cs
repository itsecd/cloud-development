using CreditApplication.Generator.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CreditApplication.Generator.Services;

/// <summary>
/// Сервис кредитных заявок с поддержкой кэширования
/// </summary>
public class CreditApplicationService(
    CreditApplicationGenerator generator,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<CreditApplicationService> logger)
{
    private readonly CreditApplicationGenerator _generator = generator;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<CreditApplicationService> _logger = logger;
    
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(
        configuration.GetValue<int>("CacheSettings:ExpirationMinutes", 5));
    private const string CacheKeyPrefix = "credit-application:";

    /// <summary>
    /// Получает кредитную заявку по ID.
    /// При первом запросе генерирует и кэширует, при повторном - возвращает из кэша.
    /// </summary>
    public async Task<CreditApplicationModel> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        _logger.LogInformation("Request for credit application with ID: {Id}", id);

        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("Credit application {Id} found in cache", id);
            var cachedApplication = JsonSerializer.Deserialize<CreditApplicationModel>(cachedData);
            
            if (cachedApplication is not null)
            {
                return cachedApplication;
            }
            
            _logger.LogWarning("Cached data for credit application {Id} deserialized to null, regenerating", id);
        }

        _logger.LogInformation("Credit application {Id} not found in cache, generating new one", id);

        var application = _generator.Generate(id);

        var serializedData = JsonSerializer.Serialize(application);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration
        };

        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);

        _logger.LogInformation(
            "Credit application {Id} saved to cache with TTL {CacheExpiration} minutes",
            id,
            _cacheExpiration.TotalMinutes);

        return application;
    }
}
