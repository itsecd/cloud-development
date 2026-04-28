using Amazon.SQS;
using Amazon.SQS.Model;
using Service.Storage.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Storage.Messaging;

/// <summary>
/// Клиентская служба для приема сообщений из очереди SQS
/// </summary>
/// <param name="sqsClient">Клиент SQS</param>
/// <param name="scopeFactory">Фабрика контекста</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
internal class SqsConsumerService(IAmazonSQS sqsClient,
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

        var queueUrlResponse = await sqsClient.GetQueueUrlAsync(_queueName, stoppingToken);
        var queueUrl = queueUrlResponse.QueueUrl;

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                }, stoppingToken);

            if (response == null)
            {
                logger.LogWarning("Received null from {queue}", _queueName);
                continue;
            }

            logger.LogInformation("Received {count} messages", response!.Messages?.Count ?? 0);

            if (response.Messages != null)
            {

                foreach (var message in response.Messages)
                {
                    try
                    {
                        logger.LogInformation("Processing message: {messageId}", message.MessageId);

                        using var scope = scopeFactory.CreateScope();
                        var s3Service = scope.ServiceProvider.GetRequiredService<S3MinioService>();
                        await s3Service.UploadFile(message.Body);

                        _ = await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
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
