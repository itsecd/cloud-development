using System.Text.Json.Serialization;

namespace CourseApp.FileService.YandexFunction;

/// <summary>
/// Элемент пакета сообщений Yandex Message Queue.
/// </summary>
public sealed class QueueEvent
{
    /// <summary>
    /// Детали события очереди.
    /// </summary>
    [JsonPropertyName("details")]
    public QueueEventDetails? Details { get; set; }
}
