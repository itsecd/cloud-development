using SoftwareProjects.Api.Entities;
using SoftwareProjects.Api.Messaging;

namespace SoftwareProjects.Api.Services;

/// <summary>
/// Реализация сервиса программных проектов с кэшированием и публикацией событий о новых проектах в брокер
/// </summary>
/// <param name="cacheService">Служба кэша Redis, через которую проверяется наличие проекта и сохраняется результат</param>
/// <param name="publisher">Служба публикации события о новом проекте в брокер сообщений (SNS)</param>
/// <param name="logger">Структурный логгер</param>
public class SoftwareProjectService(
    ISoftwareProjectCacheService cacheService,
    IProjectPublisher publisher,
    ILogger<SoftwareProjectService> logger) : ISoftwareProjectService
{
    /// <summary>
    /// Получает программный проект по идентификатору. При промахе кэша генерирует новый,
    /// публикует его в брокер для последующей сериализации в объектное хранилище и сохраняет в кэш
    /// </summary>
    /// <param name="id">Идентификатор программного проекта</param>
    /// <returns>Готовый к выдаче программный проект</returns>
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

        await publisher.Publish(project);
        await cacheService.SetToCache(id, project);

        return project;
    }
}
