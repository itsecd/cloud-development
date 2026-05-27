using System.Text.Json.Serialization;

namespace CourseApp.Api.YandexFunction;

/// <summary>
/// HTTP-событие Yandex Cloud Functions.
/// </summary>
public sealed class Request
{
    /// <summary>
    /// HTTP-метод запроса.
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Полный URL запроса.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Путь запроса.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Тело запроса.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>
    /// Query string параметры запроса.
    /// </summary>
    [JsonPropertyName("queryStringParameters")]
    public Dictionary<string, string>? QueryStringParameters { get; set; }

    /// <summary>
    /// HTTP-заголовки запроса.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Параметры пути.
    /// </summary>
    [JsonPropertyName("pathParameters")]
    public Dictionary<string, string>? PathParameters { get; set; }

    /// <summary>
    /// Признак передачи тела в Base64.
    /// </summary>
    [JsonPropertyName("isBase64Encoded")]
    public bool IsBase64Encoded { get; set; }
}
