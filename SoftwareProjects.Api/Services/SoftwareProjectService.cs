using SoftwareProjects.Api.Entities;

namespace SoftwareProjects.Api.Services;

/// <summary>
/// Реализация сервиса программных проектов с кэшированием
/// </summary>
public class SoftwareProjectService(
    ISoftwareProjectCacheService cacheService,
    ILogger<SoftwareProjectService> logger) : ISoftwareProjectService
{
    /// <summary>
    /// Получает программный проект по идентификатору из кэша или генерирует новый
    /// </summary>
    public async Task<SoftwareProject> GetById(int id)
    {
        var cached = await cacheService.GetFromCache(id);

        if (cached is not null)
            return cached;

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

        await cacheService.SetToCache(id, project);

        return project;
    }
}
