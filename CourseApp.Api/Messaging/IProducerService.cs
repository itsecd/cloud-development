using CourseApp.Api.Models;

namespace CourseApp.Api.Messaging;

/// <summary>
/// Интерфейс продюсера для отправки сгенерированных учебных курсов в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Сериализует курс в JSON и отправляет его в брокер
    /// </summary>
    /// <param name="course">Сгенерированный учебный курс</param>
    public Task SendMessage(Course course);
}
