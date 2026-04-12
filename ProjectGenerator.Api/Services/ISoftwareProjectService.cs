using ProjectGenerator.Domain.Models;

namespace ProjectGenerator.Api.Services;

/// <summary>
/// Интерфейс сервиса программных проектов
/// </summary>
public interface ISoftwareProjectService
{
    /// <summary>
    /// Получает программный проект по идентификатору из кэша или генерирует новый
    /// </summary>
    public Task<SoftwareProject> GetOrGenerate(int id);
}
