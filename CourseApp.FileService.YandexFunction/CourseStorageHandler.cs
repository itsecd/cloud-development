using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CourseApp.FileService.YandexFunction;

/// <summary>
/// Обработчик Yandex Cloud Function, вызываемый триггером Yandex Message Queue.
/// </summary>
public sealed class CourseStorageHandler
{
    private readonly IAmazonS3 _s3;
    private readonly ILogger<CourseStorageHandler> _logger;
    private readonly string _bucketName;

    /// <summary>
    /// Создаёт обработчик и настраивает клиент Yandex Object Storage через S3-compatible API.
    /// </summary>
    public CourseStorageHandler()
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        var s3Config = new AmazonS3Config
        {
            ServiceURL = config["S3_SERVICE_URL"] ?? "https://storage.yandexcloud.net",
            ForcePathStyle = true
        };

        var credentials = new BasicAWSCredentials(
            config["S3_ACCESS_KEY"] ?? string.Empty,
            config["S3_SECRET_KEY"] ?? string.Empty);

        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(credentials, s3Config));

        var provider = services.BuildServiceProvider();
        _s3 = provider.GetRequiredService<IAmazonS3>();
        _logger = provider.GetRequiredService<ILogger<CourseStorageHandler>>();
        _bucketName = config["S3_BUCKET"] ?? "courses-storage";
    }

    /// <summary>
    /// Обрабатывает пакет сообщений из очереди и сохраняет каждое сообщение в Object Storage.
    /// </summary>
    /// <param name="request">Событие триггера Yandex Message Queue.</param>
    public async Task FunctionHandler(QueueRequest request)
    {
        _logger.LogInformation("Received {Count} queue messages", request.Messages.Count);

        foreach (var item in request.Messages)
        {
            var message = item.Details?.Message;
            if (message is null || string.IsNullOrWhiteSpace(message.Body))
            {
                _logger.LogWarning("Queue message is empty");
                continue;
            }

            try
            {
                var objectKey = GetObjectKey(message.Body);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(message.Body));

                var response = await _s3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = objectKey,
                    InputStream = stream,
                    ContentType = "application/json"
                });

                if (response.HttpStatusCode != HttpStatusCode.OK)
                    throw new InvalidOperationException($"Object Storage returned {response.HttpStatusCode}");

                _logger.LogInformation("Saved queue message {MessageId} to {ObjectKey}", message.MessageId, objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process queue message {MessageId}", message.MessageId);
            }
        }
    }

    private static string GetObjectKey(string body)
    {
        var rootNode = JsonNode.Parse(body)
            ?? throw new ArgumentException("Queue message body is not valid JSON");

        var id = (rootNode["id"] ?? rootNode["Id"])?.GetValue<int>()
            ?? throw new ArgumentException("Queue message body has no course id");

        return $"course_{id}.json";
    }
}

/// <summary>
/// Событие триггера Yandex Message Queue.
/// </summary>
public sealed class QueueRequest
{
    /// <summary>
    /// Сообщения, переданные триггером.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<QueueEvent> Messages { get; set; } = [];
}

/// <summary>
/// Элемент пакета сообщений Yandex Message Queue.
/// </summary>
public sealed class QueueEvent
{
    /// <summary>
    /// Детали события очереди.
    /// </summary>
    [JsonPropertyName("details")]
    public QueueEventDetails? Details { get; set; }
}

/// <summary>
/// Детали события очереди.
/// </summary>
public sealed class QueueEventDetails
{
    /// <summary>
    /// Сообщение очереди.
    /// </summary>
    [JsonPropertyName("message")]
    public QueueMessage? Message { get; set; }
}

/// <summary>
/// Сообщение Yandex Message Queue.
/// </summary>
public sealed class QueueMessage
{
    /// <summary>
    /// Идентификатор сообщения.
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Тело сообщения.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
