using System.Text.Json;
using Generator.DTO;
using Generator.Generator;
using Microsoft.Extensions.Caching.Distributed;

namespace Generator.Service;

public class ResidentialBuildingService(
    ILogger<ResidentialBuildingService> logger,
    ResidentialBuildingGenerator generator,
    IDistributedCache cache,
    IConfiguration configuration
) : IResidentialBuildingService
{
    private const string CacheKeyPrefix = "residential-building:";

    private const int CacheExpirationTimeMinutesDefault = 15;

    private readonly TimeSpan _cacheExpirationTimeMinutes =
        TimeSpan.FromMinutes(configuration.GetValue("CacheSettings:ExpirationTimeMinutes",
            CacheExpirationTimeMinutesDefault));

    public async Task<ResidentialBuildingDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        string? jsonCached = null;
        try
        {
            jsonCached = await cache.GetStringAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to read from distributed cache for key={cacheKey}. Falling back to generation.", cacheKey);
        }

        if (!string.IsNullOrEmpty(jsonCached))
        {
            logger.LogInformation("Cache for residential building with Id={} received.", id);

            ResidentialBuildingDto? objCached = null;
            try
            {
                objCached = JsonSerializer.Deserialize<ResidentialBuildingDto>(jsonCached);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Invalid JSON in residential building cache for key {cacheKey}.", cacheKey);
            }

            if (objCached is null)
            {
                logger.LogWarning("Cache for residential building with Id={id} returned null.", id);
            }
            else
            {
                logger.LogInformation("Cache for residential building with Id={id} is valid, returned", id);
                return objCached;
            }
        }

        ResidentialBuildingDto obj = generator.Generate(id);

        try
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(obj), CreateCacheOptions(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to write residential building with Id={id} to cache. Still returning generated value.", id);
        }

        logger.LogInformation("Generated and cached residential building with Id={id}", id);

        return obj;
    }

    /// <summary>
    ///     Создаёт настройки кэша - задаёт время жизни кэша.
    /// </summary>
    private DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpirationTimeMinutes
        };
    }
}