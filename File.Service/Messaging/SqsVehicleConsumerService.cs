using Amazon.SQS;
using Amazon.SQS.Model;
using File.Service.Storage;

namespace File.Service.Messaging;

/// <summary>
/// Фоновая служба, читающая сообщения о транспортных средствах из SQS
/// и сохраняющая их в S3-хранилище
/// </summary>
/// <param name="sqsClient">Клиент Amazon SQS</param>
/// <param name="scopeFactory">Фабрика DI-скоупа (для получения <see cref="IVehicleStorageService"/>)</param>
/// <param name="configuration">Конфигурация (ключ <c>AWS:Resources:SQSQueueName</c>)</param>
/// <param name="logger">Логгер</param>
public class SqsVehicleConsumerService(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsVehicleConsumerService> logger) : BackgroundService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS vehicle consumer started for queue {Queue}", _queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueName,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                }, stoppingToken);

                if (response?.Messages == null || response.Messages.Count == 0)
                    continue;

                logger.LogInformation("Received {Count} messages from {Queue}", response.Messages.Count, _queueName);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var storage = scope.ServiceProvider.GetRequiredService<IVehicleStorageService>();
                        await storage.Upload(message.Body);
                        await sqsClient.DeleteMessageAsync(_queueName, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving messages from {Queue}", _queueName);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
