using System.Text.Json.Serialization;

namespace Cloud.GeneratorFunction.Models;

/// <summary>
/// Ответ облачной функции, возвращаемый API Gateway
/// </summary>
public class FunctionResponse
{
    /// <summary>
    /// HTTP-статус ответа
    /// </summary>
    [JsonPropertyName("statusCode")] public int StatusCode { get; set; }
    /// <summary>
    /// Заголовки ответа
    /// </summary>
    [JsonPropertyName("headers")] public Dictionary<string, string> Headers { get; set; } = new();
    /// <summary>
    /// Тело ответа
    /// </summary>
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
    /// <summary>
    /// Признак кодирования тела в Base64
    /// </summary>
    [JsonPropertyName("isBase64Encoded")] public bool IsBase64Encoded { get; set; }
}
