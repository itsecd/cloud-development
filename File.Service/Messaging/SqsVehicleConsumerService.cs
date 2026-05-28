using Amazon.SQS;
using Amazon.SQS.Model;
using File.Service.Storage;

namespace File.Service.Messaging;

/// <summary>
/// Фоновый сервис для чтения сообщений из очереди SQS и сохранения данных в S3
/// </summary>
public class SqsVehicleConsumerService(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsVehicleConsumerService> logger) : BackgroundService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new InvalidOperationException("SQS queue name is not configured");
    private string? _queueUrl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS Consumer STARTED for queue {QueueName}", _queueName);

        var getUrlResponse = await sqsClient.GetQueueUrlAsync(_queueName, stoppingToken);
        _queueUrl = getUrlResponse.QueueUrl;

        logger.LogInformation("Queue URL resolved: {QueueUrl}", _queueUrl);

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

                var messages = await sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (messages.Messages == null || messages.Messages.Count == 0)
                {
                    logger.LogInformation("No messages in queue");
                    continue;
                }

                logger.LogInformation("Received {Count} messages", messages.Messages.Count);

                foreach (var message in messages.Messages)
                {
                    logger.LogInformation("Processing message {MessageId}: {Body}",
                        message.MessageId,
                        message.Body);

                    using var scope = scopeFactory.CreateScope();
                    var storage = scope.ServiceProvider.GetRequiredService<IVehicleStorageService>();

                    var success = await storage.StoreVehicleDataAsync(message.Body);

                    logger.LogInformation("Store result for {MessageId}: {Result}",
                        message.MessageId,
                        success);

                    if (success)
                    {
                        await sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);

                        logger.LogInformation("Deleted message {MessageId}", message.MessageId);
                    }
                    else
                    {
                        logger.LogWarning("Message {MessageId} NOT deleted (store failed)", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing SQS messages");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}