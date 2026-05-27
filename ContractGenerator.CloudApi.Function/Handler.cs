using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using ContractGenerator.Shared.Generation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContractGenerator.CloudApi.Function;

/// <summary>
/// HTTP-обработчик Yandex Cloud Function для генерации сотрудников.
/// </summary>
public class Handler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmployeeGenerator _generator;
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<Handler> _logger;
    private readonly string _queueUrl;

    public Handler()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IEmployeeGenerator, BogusEmployeeGenerator>();

        var provider = services.BuildServiceProvider();
        _generator = provider.GetRequiredService<IEmployeeGenerator>();
        _logger = provider.GetRequiredService<ILogger<Handler>>();
        _queueUrl = Required(configuration, "YMQ_QUEUE_URL");

        _sqsClient = new AmazonSQSClient(
            new BasicAWSCredentials(Required(configuration, "YC_STATIC_KEY_ID"), Required(configuration, "YC_STATIC_KEY_SECRET")),
            new AmazonSQSConfig
            {
                ServiceURL = configuration["YMQ_ENDPOINT"] ?? "https://message-queue.api.cloud.yandex.net",
                AuthenticationRegion = configuration["YC_REGION"] ?? "ru-central1"
            });
    }

    /// <summary>
    /// Обрабатывает HTTP-запрос от Yandex API Gateway.
    /// </summary>
    public async Task<FunctionResponse> FunctionHandler(FunctionRequest request)
    {
        if (!TryReadId(request, out var id))
        {
            return BadRequest("Query parameter 'id' must be a positive integer");
        }

        var employee = _generator.Generate(id);
        var body = JsonSerializer.Serialize(employee, _jsonOptions);

        try
        {
            var response = await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = body
            });

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"YMQ returned {response.HttpStatusCode}");
            }

            _logger.LogInformation("Published employee {EmployeeId} to Yandex Message Queue", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish employee {EmployeeId} to Yandex Message Queue", id);
        }

        return JsonResponse(HttpStatusCode.OK, body);
    }

    private static bool TryReadId(FunctionRequest request, out int id)
    {
        id = 0;

        if (request.QueryStringParameters is null
            || !request.QueryStringParameters.TryGetValue("id", out var rawId)
            || !int.TryParse(rawId, out id))
        {
            return false;
        }

        return id > 0;
    }

    private static FunctionResponse BadRequest(string message) =>
        JsonResponse(HttpStatusCode.BadRequest, JsonSerializer.Serialize(new { error = message }, _jsonOptions));

    private static FunctionResponse JsonResponse(HttpStatusCode statusCode, string body) => new()
    {
        StatusCode = (int)statusCode,
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Access-Control-Allow-Origin"] = "*"
        },
        Body = body
    };

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"{key} is not configured");
}

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
