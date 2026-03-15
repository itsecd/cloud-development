using SoftwareProjects.Api.Entities;

namespace SoftwareProjects.Api.Services;

/// <summary>
/// Сервис получения программного проекта с поддержкой кэширования
/// </summary>
public interface ISoftwareProjectService
{
    /// <summary>
    /// Получает программный проект по идентификатору из кэша или генерирует новый
    /// </summary>
    public Task<SoftwareProject> GetById(int id);
}
