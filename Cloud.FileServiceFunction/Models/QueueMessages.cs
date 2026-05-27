using System.Text.Json.Serialization;

namespace Cloud.FileServiceFunction.Models;

/// <summary>
/// Событие, полученное из очереди Yandex Message Queue
/// </summary>
public class QueueRequest
{
    /// <summary>
    /// Список сообщений
    /// </summary>
    [JsonPropertyName("messages")] public List<QueueEvent> Messages { get; set; } = new();
}

/// <summary>
/// Единичное сообщение в пакете
/// </summary>
public class QueueEvent
{
    /// <summary>
    /// Детали сообщения
    /// </summary>
    [JsonPropertyName("details")] public QueueEventDetails? Details { get; set; }
}

/// <summary>
/// Детали сообщения, содержащие объект Message
/// </summary>
public class QueueEventDetails
{
    /// <summary>
    /// Сообщение из очереди
    /// </summary>
    [JsonPropertyName("message")] public QueueMessage? Message { get; set; }
}

/// <summary>
/// Сообщение очереди с id и body
/// </summary>
public class QueueMessage
{
    /// <summary>
    /// Идентификатор сообщения
    /// </summary>
    [JsonPropertyName("message_id")] public string MessageId { get; set; } = string.Empty;
    /// <summary>
    /// Тело сообщения (сериализованный JSON сотрудника)
    /// </summary>
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
}
