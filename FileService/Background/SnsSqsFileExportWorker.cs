using System.Text.Json;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using File.Service.Configuration;
using File.Service.Storage;
using Microsoft.Extensions.Options;

namespace File.Service.Background;

/// <summary>
/// Фоновый обработчик, читающий события из SNS через подписанную SQS-очередь,
/// сериализующий их в файлы и сохраняющий в S3.
/// </summary>
public sealed class SnsSqsFileExportWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly AwsStorageOptions _options;
    private readonly IEmployeeFileStorage _fileStorage;
    private readonly ILogger<SnsSqsFileExportWorker> _logger;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly IAmazonSQS _sqsClient;
    private readonly FileExportInfrastructureState _state;

    private string? _queueUrl;
    private string? _topicArn;

    public SnsSqsFileExportWorker(
        IOptions<AwsStorageOptions> options,
        IEmployeeFileStorage fileStorage,
        ILogger<SnsSqsFileExportWorker> logger,
        FileExportInfrastructureState state)
    {
        _options = options.Value;
        _fileStorage = fileStorage;
        _logger = logger;
        _state = state;

        var credentials = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);

        var snsConfig = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = _options.ServiceUrl,
            AuthenticationRegion = _options.Region,
            UseHttp = _options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };

        var sqsConfig = new AmazonSQSConfig
        {
            ServiceURL = _options.ServiceUrl,
            AuthenticationRegion = _options.Region,
            UseHttp = _options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        };

        _snsClient = new AmazonSimpleNotificationServiceClient(credentials, snsConfig);
        _sqsClient = new AmazonSQSClient(credentials, sqsConfig);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _state.IsInitialized = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureInfrastructureAsync(stoppingToken);
                _state.IsInitialized = true;

                _logger.LogInformation(
                    "File export infrastructure initialized. Topic={TopicArn}, Queue={QueueUrl}",
                    _topicArn,
                    _queueUrl);

                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _state.IsInitialized = false;
                _logger.LogWarning(ex, "LocalStack infrastructure is not ready yet. Retrying initialization...");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10,
                    MessageAttributeNames = ["All"],
                    MessageSystemAttributeNames = ["All"]
                }, stoppingToken);

                if (response.Messages.Count == 0)
                {
                    continue;
                }

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                    await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing messages from queue {QueueName}", _options.QueueName);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task EnsureInfrastructureAsync(CancellationToken cancellationToken)
    {
        _topicArn = (await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _options.TopicName
        }, cancellationToken)).TopicArn;

        _queueUrl = (await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = _options.QueueName
        }, cancellationToken)).QueueUrl;

        var attributes = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = _queueUrl,
            AttributeNames = ["QueueArn"]
        }, cancellationToken);

        var queueArn = attributes.Attributes["QueueArn"];

        var policy = $$"""
        {
          "Version": "2012-10-17",
          "Statement": [
            {
              "Sid": "AllowSnsPublish",
              "Effect": "Allow",
              "Principal": "*",
              "Action": "sqs:SendMessage",
              "Resource": "{{queueArn}}",
              "Condition": {
                "ArnEquals": {
                  "aws:SourceArn": "{{_topicArn}}"
                }
              }
            }
          ]
        }
        """;

        await _sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = _queueUrl,
            Attributes = new Dictionary<string, string>
            {
                ["Policy"] = policy
            }
        }, cancellationToken);

        var subscriptions = await _snsClient.ListSubscriptionsByTopicAsync(new ListSubscriptionsByTopicRequest
        {
            TopicArn = _topicArn
        }, cancellationToken);

        var existingSubscription = subscriptions.Subscriptions
            .FirstOrDefault(s => string.Equals(s.Endpoint, queueArn, StringComparison.OrdinalIgnoreCase));

        if (existingSubscription is null)
        {
            await _snsClient.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = _topicArn,
                Protocol = "sqs",
                Endpoint = queueArn,
                Attributes = new Dictionary<string, string>
                {
                    ["RawMessageDelivery"] = "true"
                }
            }, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(existingSubscription.SubscriptionArn) &&
                 !string.Equals(existingSubscription.SubscriptionArn, "PendingConfirmation", StringComparison.OrdinalIgnoreCase))
        {
            await _snsClient.SetSubscriptionAttributesAsync(new SetSubscriptionAttributesRequest
            {
                SubscriptionArn = existingSubscription.SubscriptionArn,
                AttributeName = "RawMessageDelivery",
                AttributeValue = "true"
            }, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var payloadJson = ExtractPayloadJson(message.Body);

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            _logger.LogWarning("Received empty employee export message");
            return;
        }

        var envelope = JsonSerializer.Deserialize<EmployeeGeneratedEnvelope>(payloadJson, JsonOptions);
        if (envelope is null || envelope.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            _logger.LogWarning("Received invalid employee export message: {Body}", message.Body);
            return;
        }

        var employeeJson = envelope.Payload.GetRawText();

        await _fileStorage.SaveEmployeeJsonAsync(envelope.EmployeeId, employeeJson, cancellationToken);

        _logger.LogInformation(
            "Employee {EmployeeId} exported to object storage from replica {ReplicaId}",
            envelope.EmployeeId,
            envelope.ReplicaId);
    }

    private static string? ExtractPayloadJson(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("Message", out var messageProperty) &&
                messageProperty.ValueKind == JsonValueKind.String)
            {
                return messageProperty.GetString();
            }
        }
        catch
        {
            // Если body не envelope SNS, считаем что это raw JSON.
        }

        return body;
    }

    public override void Dispose()
    {
        _snsClient.Dispose();
        _sqsClient.Dispose();
        base.Dispose();
    }

    private sealed class EmployeeGeneratedEnvelope
    {
        public int EmployeeId { get; init; }

        public DateTime PublishedAtUtc { get; init; }

        public string ReplicaId { get; init; } = string.Empty;

        public JsonElement Payload { get; init; }
    }
}