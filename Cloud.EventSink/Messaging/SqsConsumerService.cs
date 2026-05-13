using Amazon.SQS;
using Amazon.SQS.Model;
using Cloud.EventSink.S3;

namespace Cloud.EventSink.Messaging;

/// <summary>
/// Фоновая служба, читающая SQS сообщения и сохраняющая их в S3.
/// </summary>
/// <param name="sqsClient">Клиент SQS</param>
/// <param name="scopeFactory">Фабрика scope для создания экземпляров сервисов на каждое сообщение</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public sealed class SqsConsumerService(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsConsumerService> logger
    ) : BackgroundService
{
    private readonly string _queueUrl = configuration["AWS:Resources:SQSQueueUrl"]
        ?? throw new KeyNotFoundException("SQS queue URL not found in configuration.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var startupScope = scopeFactory.CreateScope())
        {
            var s3 = startupScope.ServiceProvider.GetRequiredService<IS3Service>();
            await s3.EnsureBucketExists();
        }

        logger.LogInformation("SQS consumer started, polling queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            }, stoppingToken);

            if (response?.Messages is null || response.Messages.Count == 0)
                continue;

            foreach (var message in response.Messages)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var s3 = scope.ServiceProvider.GetRequiredService<IS3Service>();
                    await s3.UploadFile(message.Body);
                    await sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                    logger.LogInformation("Processed message {MessageId}", message.MessageId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process message {MessageId}", message.MessageId);
                }
            }
        }
    }
}