using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SoftwareProjects.Api.Entities;

namespace SoftwareProjects.Api.Services;

/// <summary>
/// Реализация сервиса программных проектов с кэшированием через Redis
/// </summary>
public class SoftwareProjectService(
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<SoftwareProjectService> logger) : ISoftwareProjectService
{
    private readonly int _cacheExpirationMinutes = configuration.GetValue<int>("CacheExpirationMinutes", 5);

    /// <summary>
    /// Получает программный проект по идентификатору из кэша или генерирует новый
    /// </summary>
    public async Task<SoftwareProject> GetById(int id)
    {
        var cacheKey = $"software-project-{id}";

        try
        {
            var cached = await cache.GetStringAsync(cacheKey);

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

        SoftwareProject project;

        try
        {
            project = SoftwareProjectFaker.Generate(id);
            logger.LogInformation("Project {ProjectId} generated successfully", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate project {ProjectId}", id);
            throw;
        }

        try
        {
            var json = JsonSerializer.Serialize(project);
            await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
            });
            logger.LogInformation("Project {ProjectId} saved to cache", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write project {ProjectId} to cache", id);
        }

        return project;
    }
}
