using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProjectGenerator.Domain.Models;

namespace ProjectGenerator.Api.Services;

/// <summary>
/// Сервис программных проектов с кэшированием
/// </summary>
/// <param name="generator">Генератор программных проектов</param>
/// <param name="cache">Распределённый кэш</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public class SoftwareProjectService(
    ISoftwareProjectGenerator generator,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<SoftwareProjectService> logger) : ISoftwareProjectService
{
    private const string CacheKeyPrefix = "software-project";
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(configuration.GetValue("CacheTtlMinutes", 15));

    /// <inheritdoc />
    public async Task<SoftwareProject> GetOrGenerate(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}:{id}";

        var cached = await GetFromCache(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        logger.LogInformation("Cache miss for id {Id}, generating new data", id);

        var project = generator.Generate(id);

        await SetToCache(cacheKey, project);

        return project;
    }

    /// <summary>
    /// Получение программного проекта из кэша
    /// </summary>
    /// <param name="cacheKey">Ключ кэша</param>
    /// <returns>Программный проект или null</returns>
    private async Task<SoftwareProject?> GetFromCache(string cacheKey)
    {
        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached is null)
            {
                return null;
            }

            logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<SoftwareProject>(cached);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Сохранение программного проекта в кэш
    /// </summary>
    /// <param name="cacheKey">Ключ кэша</param>
    /// <param name="project">Программный проект</param>
    private async Task SetToCache(string cacheKey, SoftwareProject project)
    {
        try
        {
            var json = JsonSerializer.Serialize(project);
            await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl
            });

            logger.LogInformation("Cached data for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write to cache for key {CacheKey}", cacheKey);
        }
    }
}
