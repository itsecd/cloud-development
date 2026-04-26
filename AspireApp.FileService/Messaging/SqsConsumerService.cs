using Amazon.SQS;
using Amazon.SQS.Model;
using AspireApp.FileService.Storage;

namespace AspireApp.FileService.Messaging;

/// <summary>
/// Клиентская служба-потребитель сообщений из очереди SQS
/// </summary>
/// <param name="sqsClient">AWS SDK клиент SQS</param>
/// <param name="scopeFactory">Фабрика DI scope</param>
/// <param name="configuration">Конфигурация приложения, содержит имя очереди</param>
/// <param name="logger">Логгер</param>
public class SqsConsumerService(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsConsumerService> logger) : BackgroundService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("Имя SQS очереди не найдено в конфигурации");

    /// <summary>
    /// Основной цикл потребителя. Принимает батчи по 10 сообщений с long polling
    /// </summary>
    /// <param name="stoppingToken">Токен остановки фонового сервиса</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Служба-потребитель SQS запущена");

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
                logger.LogWarning("Получен пустой ответ из очереди {Queue}", _queueName);
                continue;
            }

            logger.LogInformation("Получено {Count} сообщений", response.Messages?.Count ?? 0);

            if (response.Messages != null)
            {
                foreach (var message in response.Messages)
                {
                    try
                    {
                        logger.LogInformation("Обработка сообщения {MessageId}", message.MessageId);

                        using var scope = scopeFactory.CreateScope();
                        var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
                        await s3Service.UploadFile(message.Body);

                        _ = await sqsClient.DeleteMessageAsync(_queueName, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка обработки сообщения {MessageId}", message.MessageId);
                        continue;
                    }
                }
                logger.LogInformation("Батч из {Count} сообщений обработан", response.Messages.Count);
            }
        }
    }
}
