using System.Text.Json.Serialization;

namespace Cloud.GeneratorFunction.Models;

/// <summary>
/// Запрос, получаемый облачной функцией от API Gateway
/// </summary>
public class FunctionRequest
{
    /// <summary>
    /// Путь запроса
    /// </summary>
    [JsonPropertyName("path")] public string? Path { get; set; }
    /// <summary>
    /// Полный URL запроса
    /// </summary>
    [JsonPropertyName("url")] public string? Url { get; set; }
    /// <summary>
    /// Параметры строки запроса
    /// </summary>
    [JsonPropertyName("queryStringParameters")] public Dictionary<string, string>? QueryStringParameters { get; set; }
    /// <summary>
    /// Параметры пути
    /// </summary>
    [JsonPropertyName("pathParameters")] public Dictionary<string, string>? PathParameters { get; set; }
    /// <summary>
    /// Альтернативное представление параметров пути
    /// </summary>
    [JsonPropertyName("pathParams")] public Dictionary<string, string>? PathParams { get; set; }
}
