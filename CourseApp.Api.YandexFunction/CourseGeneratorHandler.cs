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

/// <summary>
/// HTTP-обработчик Yandex Cloud Function для генерации учебного курса.
/// </summary>
public sealed class CourseGeneratorHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAmazonSQS _sqs;
    private readonly ILogger<CourseGeneratorHandler> _logger;
    private readonly string _queueUrl;

    /// <summary>
    /// Создаёт обработчик и настраивает клиент Yandex Message Queue через SQS-compatible API.
    /// </summary>
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

    /// <summary>
    /// Обрабатывает HTTP-запрос, генерирует курс и публикует его JSON в очередь сообщений.
    /// </summary>
    /// <param name="request">HTTP-событие, переданное Yandex Cloud Functions.</param>
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

        if (id < 0)
            return Error(HttpStatusCode.BadRequest, "ID must not be negative");

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
