using Amazon.SQS;
using Amazon.SQS.Model;
using File.Service.Storage;

namespace File.Service.Messaging;

/// <summary>
/// Клиентская служба для приёма сообщений с пациентами из очереди SQS
/// </summary>
/// <param name="sqsClient">Клиент SQS</param>
/// <param name="scopeFactory">Фабрика областей служб</param>
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

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS consumer service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = _queueName,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                }, stoppingToken);

            if (response == null)
            {
                logger.LogWarning("Received null from {queue}", _queueName);
                continue;
            }

            if (response.Messages is { Count: > 0 })
            {
                logger.LogInformation("Received {count} messages", response.Messages.Count);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        logger.LogInformation("Processing message: {messageId}", message.MessageId);

                        using var scope = scopeFactory.CreateScope();
                        var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
                        await s3Service.UploadFile(message.Body);

                        await sqsClient.DeleteMessageAsync(_queueName, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing message: {messageId}", message.MessageId);
                        continue;
                    }
                }
                logger.LogInformation("Batch of {count} messages processed", response.Messages.Count);
            }
        }
    }
}
