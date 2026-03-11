using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using WarehouseItem.Generator.DTO;
using WarehouseItem.Generator.Generator;

namespace WarehouseItem.Generator.Service;

public sealed class WarehouseItemService(
    ILogger<WarehouseItemService> logger,
    WarehouseItemGenerator generator,
    IDistributedCache cache,
    IConfiguration configuration) : IWarehouseItemService
{
    private const string CacheKeyPrefix = "warehouse-item:";
    private const int CacheExpirationTimeMinutesDefault = 15;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(
        configuration.GetValue("CacheSettings:ExpirationTimeMinutes", CacheExpirationTimeMinutesDefault));

    public async Task<WarehouseItemDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        var cached = await TryReadCacheAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var generated = generator.Generate(id);
        await TryWriteCacheAsync(cacheKey, generated, cancellationToken);

        return generated;
    }

    private async Task<WarehouseItemDto?> TryReadCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
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

    private async Task TryWriteCacheAsync(string cacheKey, WarehouseItemDto value, CancellationToken cancellationToken)
    {
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
