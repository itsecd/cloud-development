using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CourseApp.Api.YandexFunction;

public sealed class CourseGeneratorHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAmazonSQS _sqs;
    private readonly ILogger<CourseGeneratorHandler> _logger;
    private readonly string _queueUrl;

    public CourseGeneratorHandler()
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(config);

        var sqsConfig = new AmazonSQSConfig
        {
            ServiceURL = config["SQS_SERVICE_URL"] ?? "https://message-queue.api.cloud.yandex.net",
            AuthenticationRegion = config["SQS_REGION"] ?? "ru-central1"
        };

        var credentials = new BasicAWSCredentials(
            config["SQS_ACCESS_KEY"] ?? string.Empty,
            config["SQS_SECRET_KEY"] ?? string.Empty);

        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(credentials, sqsConfig));

        var provider = services.BuildServiceProvider();
        _sqs = provider.GetRequiredService<IAmazonSQS>();
        _logger = provider.GetRequiredService<ILogger<CourseGeneratorHandler>>();
        _queueUrl = config["SQS_QUEUE_URL"]
            ?? throw new InvalidOperationException("SQS_QUEUE_URL is not configured");
    }

    public async Task<Response> FunctionHandler(Request request)
    {
        if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            return Error(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");

        if (request.QueryStringParameters is null
            || !request.QueryStringParameters.TryGetValue("id", out var idValue)
            || !int.TryParse(idValue, out var id))
        {
            return Error(HttpStatusCode.BadRequest, "Missing or invalid 'id' query parameter");
        }

        if (id <= 0)
            return Error(HttpStatusCode.BadRequest, "ID must be greater than 0");

        var course = CourseGenerator.Generate(id);
        var body = JsonSerializer.Serialize(course, JsonOptions);

        try
        {
            var response = await _sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = body
            });

            _logger.LogInformation("Published course {CourseId}, message {MessageId}", id, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish course {CourseId} to Yandex Message Queue", id);
        }

        return new Response
        {
            StatusCode = 200,
            Headers = JsonHeaders(),
            Body = body
        };
    }

    private static Response Error(HttpStatusCode statusCode, string message) => new()
    {
        StatusCode = (int)statusCode,
        Headers = TextHeaders(),
        Body = message
    };

    private static Dictionary<string, string> JsonHeaders() => new()
    {
        ["Content-Type"] = "application/json",
        ["Access-Control-Allow-Origin"] = "*"
    };

    private static Dictionary<string, string> TextHeaders() => new()
    {
        ["Content-Type"] = "text/plain; charset=utf-8",
        ["Access-Control-Allow-Origin"] = "*"
    };
}

public sealed class Request
{
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("queryStringParameters")]
    public Dictionary<string, string>? QueryStringParameters { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("pathParameters")]
    public Dictionary<string, string>? PathParameters { get; set; }

    [JsonPropertyName("isBase64Encoded")]
    public bool IsBase64Encoded { get; set; }
}

public sealed class Response
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
