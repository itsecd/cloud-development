using SoftwareProjects.Api.Entities;

namespace SoftwareProjects.Api.Services;

/// <summary>
/// Служба кэширования программных проектов
/// </summary>
public interface ISoftwareProjectCacheService
{
    /// <summary>
    /// Получает программный проект из кэша по идентификатору
    /// </summary>
    public Task<SoftwareProject?> GetFromCache(int id);

    /// <summary>
    /// Сохраняет программный проект в кэш
    /// </summary>
    public Task SetToCache(int id, SoftwareProject project);
}
