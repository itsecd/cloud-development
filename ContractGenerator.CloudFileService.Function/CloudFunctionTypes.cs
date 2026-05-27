using System.Text.Json.Serialization;

namespace ContractGenerator.CloudFileService.Function;

public class FunctionRequest
{
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("queryStringParameters")]
    public Dictionary<string, string>? QueryStringParameters { get; set; }

    [JsonPropertyName("pathParameters")]
    public Dictionary<string, string>? PathParameters { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("isBase64Encoded")]
    public bool IsBase64Encoded { get; set; }
}

public class FunctionResponse
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("isBase64Encoded")]
    public bool IsBase64Encoded { get; set; }
}

public class QueueRequest
{
    [JsonPropertyName("messages")]
    public List<QueueEvent> Messages { get; set; } = [];
}

public class QueueEvent
{
    [JsonPropertyName("details")]
    public QueueEventDetails? Details { get; set; }
}

public class QueueEventDetails
{
    [JsonPropertyName("message")]
    public QueueMessage? Message { get; set; }
}

public class QueueMessage
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
