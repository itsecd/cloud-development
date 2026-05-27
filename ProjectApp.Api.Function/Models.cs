using System.Text.Json.Serialization;

namespace ProjectApp.Api.Function;

public class CreditApplication
{
    public int Id { get; set; }
    public string CreditType { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public double InterestRate { get; set; }
    public DateOnly ApplicationDate { get; set; }
    public bool RequiresInsurance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DecisionDate { get; set; }
    public decimal? ApprovedAmount { get; set; }
}

public class CreditApplicationGeneratedEvent
{
    public int Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public CreditApplication Application { get; set; } = new();
}

public class FunctionRequest
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("queryStringParameters")]
    public Dictionary<string, string>? QueryStringParameters { get; set; }

    [JsonPropertyName("pathParameters")]
    public Dictionary<string, string>? PathParameters { get; set; }

    [JsonPropertyName("pathParams")]
    public Dictionary<string, string>? PathParams { get; set; }
}

public class FunctionResponse
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("isBase64Encoded")]
    public bool IsBase64Encoded { get; set; }
}
