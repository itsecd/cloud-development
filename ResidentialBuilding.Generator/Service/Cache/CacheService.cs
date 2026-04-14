using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Generator.Service.Cache;

/// <summary>
/// Реализация <see cref="ICacheService"/> на базе <see cref="IDistributedCache"/> (Redis).
/// </summary>
public class CacheService(
    ILogger<ResidentialBuildingService> logger,
    IDistributedCache cache,
    IConfiguration configuration) : ICacheService
{
    private const string CacheKeyPrefix = "residential-building:";

    private const int CacheExpirationTimeMinutesDefault = 15;

    private readonly TimeSpan _cacheExpirationTimeMinutes =
        TimeSpan.FromMinutes(configuration.GetValue("CacheSettings:ExpirationTimeMinutes",
            CacheExpirationTimeMinutesDefault));
    
    /// <inheritdoc />
    public async Task<T?> GetCache<T>(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        string? jsonCached;
        try
        {
            jsonCached = await cache.GetStringAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read from distributed cache for key={cacheKey}.", cacheKey);
            return default;
        }

        if (string.IsNullOrEmpty(jsonCached))
        {
            logger.LogWarning("Received cache for key={cacheKey} is null or empty.", cacheKey);
            return default;
        }
        
        T? objCached;
        try
        {
            objCached = JsonSerializer.Deserialize<T>(jsonCached);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid JSON in cache for key {cacheKey}.", cacheKey);
            return default;
        }
        
        if (objCached is null)
        {
            logger.LogWarning("Cache for key {cacheKey} returned null.", cacheKey);
            return default;
        }
        
        logger.LogInformation("Cache for cache key {cacheKey} is valid, returned", cacheKey);
        return objCached;
    }

    /// <inheritdoc />
    public async Task<bool> SetCache<T>(int id, T obj, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpirationTimeMinutes
            };
            
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(obj), cacheOptions, cancellationToken);
            return true;
        }
        catch(Exception ex)
        {
            logger.LogWarning(ex, "Failed to write object for cache key {cacheKey}.", cacheKey);
            return false;
        }
    }
}