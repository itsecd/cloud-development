using Amazon.SQS;
using Amazon.SQS.Model;
using File.Service.Storage;

namespace File.Service.Messaging;

public class SqsVehicleConsumerService : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _queueName;
    private readonly ILogger<SqsVehicleConsumerService> _logger;
    private string? _queueUrl;

    public SqsVehicleConsumerService(
        IAmazonSQS sqsClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SqsVehicleConsumerService> logger)
    {
        _sqsClient = sqsClient;
        _scopeFactory = scopeFactory;
        _queueName = configuration["AWS:Resources:SQSQueueName"]
            ?? throw new InvalidOperationException("SQS queue name is not configured");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting SQS consumer for queue {QueueName}", _queueName);

        // Получаем URL очереди
        var getUrlResponse = await _sqsClient.GetQueueUrlAsync(_queueName, stoppingToken);
        _queueUrl = getUrlResponse.QueueUrl;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10,
                    VisibilityTimeout = 30
                };

                var messages = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (messages.Messages == null || messages.Messages.Count == 0)
                    continue;

                _logger.LogInformation("Received {Count} messages from queue", messages.Messages.Count);

                foreach (var message in messages.Messages)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var storage = scope.ServiceProvider.GetRequiredService<IVehicleStorageService>();

                    var success = await storage.StoreVehicleDataAsync(message.Body);

                    if (success)
                    {
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                        _logger.LogInformation("Message {MessageId} processed and deleted", message.MessageId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to process message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SQS messages");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}