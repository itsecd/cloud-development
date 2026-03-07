using ServiceApi.Entities;

namespace ServiceApi.Generator;

/// <summary>
/// Интерфейс для работы с кэшем проектов
/// </summary>
public interface IProgramProjectCache
{
    /// <summary>
    /// Получить проект из кэша по id
    /// </summary>
    Task<ProgramProject?> GetProjectFromCache(int id);

    /// <summary>
    /// Сохранить проект в кэш
    /// </summary>
    Task SaveProjectToCache(ProgramProject programProject);
}
