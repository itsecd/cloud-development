using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WarehouseItem.Generator.DTO;

namespace WarehouseItem.Generator.Service;

/// <summary>
/// Реализация кэширования товаров с использованием распределенного кэша.
/// </summary>
public sealed class WarehouseItemCache(
    ILogger<WarehouseItemCache> logger,
    IDistributedCache cache,
    IConfiguration configuration) : IWarehouseItemCache
{
    private const string CacheKeyPrefix = "warehouse-item:";
    private const int CacheExpirationTimeMinutesDefault = 15;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(
        configuration.GetValue("CacheSettings:ExpirationTimeMinutes", CacheExpirationTimeMinutesDefault));

    /// <summary>
    /// Получить товар из кэша по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO товара или null, если не найден в кэше.</returns>
    public async Task<WarehouseItemDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        string? json;
        try
        {
            json = await cache.GetStringAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for key={cacheKey}.", cacheKey);
            return null;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            logger.LogInformation("Cache miss for key={cacheKey}.", cacheKey);
            return null;
        }

        try
        {
            var obj = JsonSerializer.Deserialize<WarehouseItemDto>(json, _jsonOptions);
            if (obj is null)
            {
                logger.LogWarning("Cache value for key={cacheKey} deserialized as null.", cacheKey);
                return null;
            }

            logger.LogInformation("Cache hit for id={id}.", obj.Id);
            return obj;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache JSON invalid for key={cacheKey}.", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Сохранить товар в кэш.
    /// </summary>
    /// <param name="id">Идентификатор товара.</param>
    /// <param name="value">DTO товара для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task SetAsync(int id, WarehouseItemDto value, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl
            };

            var json = JsonSerializer.Serialize(value, _jsonOptions);
            await cache.SetStringAsync(cacheKey, json, options, cancellationToken);
            logger.LogInformation("Cached id={id} for ttl={ttlMinutes}m.", value.Id, _cacheTtl.TotalMinutes);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for id={id}.", value.Id);
        }
    }
}