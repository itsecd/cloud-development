using System.Text.Json.Serialization;

namespace CourseApp.FileService.YandexFunction;

/// <summary>
/// Детали события очереди.
/// </summary>
public sealed class QueueEventDetails
{
    /// <summary>
    /// Сообщение очереди.
    /// </summary>
    [JsonPropertyName("message")]
    public QueueMessage? Message { get; set; }
}
