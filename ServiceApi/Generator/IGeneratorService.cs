using ServiceApi.Entities;

namespace ServiceApi.Generator;

/// <summary>
/// Интерфейс для запуска usecase по обработке программных проектов
/// </summary>
public interface IGeneratorService
{
    /// <summary>
    /// Обработка запроса на генерации программного проекта
    /// </summary>
    /// <param name="id"> Идентификатор </param>
    /// <returns>Программный проект</returns>
    public Task<ProgramProject> ProcessProgramProject(int id);
    
}
