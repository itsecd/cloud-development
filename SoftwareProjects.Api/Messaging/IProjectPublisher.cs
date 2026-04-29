using SoftwareProjects.Api.Entities;

namespace SoftwareProjects.Api.Messaging;

/// <summary>
/// Интерфейс службы публикации сгенерированных программных проектов в брокер сообщений
/// </summary>
public interface IProjectPublisher
{
    /// <summary>
    /// Публикует сериализованное представление программного проекта в брокер
    /// </summary>
    /// <param name="project">Программный проект, который должен быть передан в файловый сервис</param>
    /// <returns>Задача, завершающаяся после публикации</returns>
    public Task Publish(SoftwareProject project);
}
