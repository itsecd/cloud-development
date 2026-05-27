using Amazon.SQS;
using Amazon.SQS.Model;
using ContractGenerator.FileService.Storage;

namespace ContractGenerator.FileService.Messaging;

/// <summary>
/// Фоновый обработчик сообщений SQS, сохраняющий сотрудников в S3.
/// </summary>
/// <param name="sqsClient">Клиент Amazon SQS.</param>
/// <param name="scopeFactory">Фабрика DI-скоупов.</param>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="logger">Логгер.</param>
public class SqsEmployeeConsumerService(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsEmployeeConsumerService> logger) : BackgroundService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    private string? _queueUrl;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS employee consumer started for queue {QueueName}", _queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueUrl = await GetQueueUrlAsync(stoppingToken);
                var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                }, stoppingToken);

                if (response.Messages is null || response.Messages.Count == 0)
                {
                    continue;
                }

                logger.LogInformation("Received {MessageCount} employee messages from SQS", response.Messages.Count);

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(queueUrl, message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to receive employee messages from SQS queue {QueueName}", _queueName);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string queueUrl, Message message, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IEmployeeFileStorage>();

            await storage.SaveEmployeeJsonAsync(message.Body, cancellationToken);
            await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);

            logger.LogInformation("Processed employee message {MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process employee message {MessageId}", message.MessageId);
        }
    }

    private async Task<string> GetQueueUrlAsync(CancellationToken cancellationToken)
    {
        if (_queueUrl is not null)
        {
            return _queueUrl;
        }

        var response = await sqsClient.GetQueueUrlAsync(_queueName, cancellationToken);
        _queueUrl = response.QueueUrl;
        return _queueUrl;
    }
}
