using Amazon.SQS;
using Amazon.SQS.Model;
using File.Service.Storage;

namespace File.Service.Messaging;

/// <summary>
/// Фоновый сервис, потребляющий сообщения из SQS и сохраняющий их в файловое хранилище
/// </summary>
/// <param name="sqsClient">Клиент SQS</param>
/// <param name="scopeFactory">Фабрика DI-областей</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public sealed class SqsConsumerService(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsConsumerService> logger) : BackgroundService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS consumer service started for queue {queue}", _queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueName,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 5
            }, stoppingToken);

            if (response?.Messages is null || response.Messages.Count == 0)
                continue;

            logger.LogInformation("Received {count} messages from {queue}", response.Messages.Count, _queueName);

            foreach (var message in response.Messages)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
                    await storage.UploadAsync(message.Body);
                    await sqsClient.DeleteMessageAsync(_queueName, message.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message {id}", message.MessageId);
                }
            }
        }
    }
}
