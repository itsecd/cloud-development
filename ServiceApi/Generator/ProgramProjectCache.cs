using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ServiceApi.Entities;

namespace ServiceApi.Generator;

public class ProgramProjectCache(IDistributedCache cache, IConfiguration configuration, ILogger<GeneratorService> logger) : IProgramProjectCache
{
    /// <summary>
    /// Время инвализации кэша
    /// </summary>
    private readonly TimeSpan _cacheExpiration = int.TryParse(configuration["CacheExpiration"], out var sec)
        ? TimeSpan.FromSeconds(sec)
        : TimeSpan.FromSeconds(3600);

    /// <summary>
    /// Получить ПП из кэша по id
    /// </summary>
    /// <param name="id">Идентификатор ПП</param>
    /// <returns>Программный проект</returns>
    public async Task<ProgramProject?> GetProjectFromCache(int id)
    {
        var json = await cache.GetStringAsync(id.ToString());
        if (string.IsNullOrEmpty(json))
        {
            logger.LogWarning("Не найден проект с {id} в кэше", id);
            return null;
        }
        logger.LogInformation("Проект с {id} был найден в кэше", id);
        return JsonSerializer.Deserialize<ProgramProject>(json);
    }

    /// <summary>
    /// Кладет ПП в кэш
    /// </summary>
    /// <param name="programProject">Программный проект</param>
    public async Task SaveProjectToCache(ProgramProject programProject)
    {
        logger.LogInformation("Проект с {id} добавлен в кэш", programProject.Id);
        var json = JsonSerializer.Serialize(programProject);
        await cache.SetStringAsync(programProject.Id.ToString(), json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            });
    }
}