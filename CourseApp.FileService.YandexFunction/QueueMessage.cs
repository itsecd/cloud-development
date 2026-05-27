using System.Text.Json.Serialization;

namespace CourseApp.FileService.YandexFunction;

/// <summary>
/// Сообщение Yandex Message Queue.
/// </summary>
public sealed class QueueMessage
{
    /// <summary>
    /// Идентификатор сообщения.
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Тело сообщения.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
