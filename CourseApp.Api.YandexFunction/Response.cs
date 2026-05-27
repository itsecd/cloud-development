using System.Text.Json.Serialization;

namespace CourseApp.Api.YandexFunction;

/// <summary>
/// HTTP-ответ Yandex Cloud Functions.
/// </summary>
public sealed class Response
{
    /// <summary>
    /// HTTP-статус ответа.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// HTTP-заголовки ответа.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Тело ответа.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
