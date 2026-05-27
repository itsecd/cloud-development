using System.Text.Json.Serialization;

namespace CourseApp.FileService.YandexFunction;

/// <summary>
/// Событие триггера Yandex Message Queue.
/// </summary>
public sealed class QueueRequest
{
    /// <summary>
    /// Сообщения, переданные триггером.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<QueueEvent> Messages { get; set; } = [];
}
