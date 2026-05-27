using Amazon.S3;
using Amazon.S3.Model;
using ContractGenerator.Shared.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ContractGenerator.CloudFileService.Function;

/// <summary>
/// Обработчик YMQ-триггера, сохраняющий сотрудников в Object Storage.
/// </summary>
public class QueueHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<QueueHandler> _logger;
    private readonly string _bucketName;

    public QueueHandler()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        var provider = services.BuildServiceProvider();

        _logger = provider.GetRequiredService<ILogger<QueueHandler>>();
        _s3Client = YandexStorageFactory.CreateClient(configuration);
        _bucketName = YandexStorageFactory.GetBucketName(configuration);
    }

    /// <summary>
    /// Обрабатывает пачку сообщений из Yandex Message Queue.
    /// </summary>
    public async Task FunctionHandler(QueueRequest request)
    {
        _logger.LogInformation("Received {MessageCount} employee messages", request.Messages.Count);

        foreach (var queueEvent in request.Messages)
        {
            var message = queueEvent.Details?.Message;
            if (message is null)
            {
                _logger.LogWarning("Queue event has no message payload");
                continue;
            }

            await SaveEmployeeAsync(message.Body);
            _logger.LogInformation("Processed YMQ message {MessageId}", message.MessageId);
        }
    }

    private async Task SaveEmployeeAsync(string employeeJson)
    {
        var rootNode = JsonNode.Parse(employeeJson)
            ?? throw new ArgumentException("Employee message body is not a valid JSON", nameof(employeeJson));
        var id = rootNode["id"]?.GetValue<int>()
            ?? rootNode["Id"]?.GetValue<int>()
            ?? throw new ArgumentException("Employee message has no id field", nameof(employeeJson));

        var key = EmployeeFileKeys.ForId(id);
        var normalizedJson = rootNode.ToJsonString(_jsonOptions);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(normalizedJson));
        var response = await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json",
            UseChunkEncoding = false,
            AutoCloseStream = false
        });

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Object Storage returned {response.HttpStatusCode} while saving {key}");
        }

        _logger.LogInformation("Saved employee {EmployeeId} to Object Storage object {Key}", id, key);
    }
}
