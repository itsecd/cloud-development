using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SoftwareProjects.Api.Entities;

namespace SoftwareProjects.Api.Services;

/// <summary>
/// Реализация службы кэширования программных проектов через Redis
/// </summary>
public class SoftwareProjectCacheService(
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<SoftwareProjectCacheService> logger) : ISoftwareProjectCacheService
{
    private readonly int _cacheExpirationMinutes = configuration.GetValue<int>("CacheExpirationMinutes", 5);

    /// <summary>
    /// Получает программный проект из кэша по идентификатору
    /// </summary>
    public async Task<SoftwareProject?> GetFromCache(int id)
    {
        try
        {
            var cached = await cache.GetStringAsync($"software-project-{id}");

            if (!string.IsNullOrEmpty(cached))
            {
                var deserialized = JsonSerializer.Deserialize<SoftwareProject>(cached);

                if (deserialized is not null)
                {
                    logger.LogInformation("Project {ProjectId} retrieved from cache", id);
                    return deserialized;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read project {ProjectId} from cache", id);
        }

        return null;
    }

    /// <summary>
    /// Сохраняет программный проект в кэш
    /// </summary>
    public async Task SetToCache(int id, SoftwareProject project)
    {
        try
        {
            var json = JsonSerializer.Serialize(project);
            await cache.SetStringAsync($"software-project-{id}", json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
            });
            logger.LogInformation("Project {ProjectId} saved to cache", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write project {ProjectId} to cache", id);
        }
    }
}
