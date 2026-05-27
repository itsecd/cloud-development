using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Cloud.GeneratorFunction;

public class Handler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public FunctionResponse FunctionHandler(FunctionRequest request)
    {
        var id = TryReadId(request);
        if (id is null or <= 0)
        {
            return CreateResponse(400, """{"error":"id must be a positive integer"}""");
        }

        var generator = new EmployeeGenerator();
        var employee = generator.Generate(id.Value);
        PublishGeneratedAsync(employee).GetAwaiter().GetResult();

        return CreateResponse(200, JsonSerializer.Serialize(employee, _jsonOptions));
    }

    private static int? TryReadId(FunctionRequest? request)
    {
        try
        {
            var rawId = TryGetValue(request?.QueryStringParameters, "id")
                        ?? TryGetValue(request?.PathParameters, "id")
                        ?? TryGetValue(request?.PathParams, "id");

            if (int.TryParse(rawId, out var id))
                return id;

            // Пытаемся достать из query строки, если она в path
            var path = request?.Path ?? request?.Url ?? string.Empty;
            var queryStart = path.IndexOf('?');
            if (queryStart >= 0)
            {
                var query = path[queryStart..];
                return ReadIdFromQuery(query);
            }
        }
        catch { }
        return null;
    }

    private static int? ReadIdFromQuery(string query)
    {
        var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 &&
                parts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(Uri.UnescapeDataString(parts[1]), out var id))
            {
                return id;
            }
        }
        return null;
    }

    private static string? TryGetValue(Dictionary<string, string>? values, string key)
    {
        if (values is null) return null;
        return values.TryGetValue(key, out var value) ? value : null;
    }

    private static async Task PublishGeneratedAsync(Employee employee)
    {
        var queueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrWhiteSpace(queueUrl) ||
            string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey))
        {
            Console.WriteLine("[WARN] Message Queue settings are not configured");
            return;
        }

        var endpoint = Environment.GetEnvironmentVariable("SQS_ENDPOINT")
                       ?? "https://message-queue.api.cloud.yandex.net";
        var region = Environment.GetEnvironmentVariable("YC_REGION") ?? "ru-central1";

        using var sqs = new AmazonSQSClient(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonSQSConfig
            {
                ServiceURL = endpoint,
                AuthenticationRegion = region
            });

        var messageBody = JsonSerializer.Serialize(employee, _jsonOptions);
        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody
        });
    }

    private static FunctionResponse CreateResponse(int statusCode, string body) =>
        new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Methods"] = "GET,OPTIONS",
                ["Access-Control-Allow-Headers"] = "Content-Type,Authorization"
            },
            Body = body
        };
}

public class FunctionRequest
{
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("queryStringParameters")] public Dictionary<string, string>? QueryStringParameters { get; set; }
    [JsonPropertyName("pathParameters")] public Dictionary<string, string>? PathParameters { get; set; }
    [JsonPropertyName("pathParams")] public Dictionary<string, string>? PathParams { get; set; }
}

public class FunctionResponse
{
    [JsonPropertyName("statusCode")] public int StatusCode { get; set; }
    [JsonPropertyName("headers")] public Dictionary<string, string> Headers { get; set; } = new();
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
    [JsonPropertyName("isBase64Encoded")] public bool IsBase64Encoded { get; set; }
}