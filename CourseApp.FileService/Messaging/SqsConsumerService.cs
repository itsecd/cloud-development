using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using CourseApp.Api.Models;
using CourseApp.FileService.Storage;

namespace CourseApp.FileService.Messaging;

/// <summary>
/// Фоновая служба, читающая SQS батчами и складывающая курсы в S3.
/// Перед стартом цикла гарантирует существование бакета.
/// </summary>
/// <param name="sqsClient">Клиент SQS</param>
/// <param name="scopeFactory">Фабрика scope для получения IS3Service на каждое сообщение</param>
/// <param name="configuration">Конфигурация (имя очереди)</param>
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
        using (var startupScope = scopeFactory.CreateScope())
        {
            var s3 = startupScope.ServiceProvider.GetRequiredService<IS3Service>();
            await s3.EnsureBucketExists();
        }

        logger.LogInformation("SQS consumer started, polling {Queue}", _queueName);

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

            foreach (var message in response.Messages)
            {
                try
                {
                    var course = JsonSerializer.Deserialize<Course>(message.Body)
                        ?? throw new InvalidOperationException("Message body is not a valid Course");

                    using var scope = scopeFactory.CreateScope();
                    var s3 = scope.ServiceProvider.GetRequiredService<IS3Service>();
                    await s3.UploadFile(course);

                    await sqsClient.DeleteMessageAsync(_queueName, message.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process SQS message {MessageId}", message.MessageId);
                }
            }
        }
    }
}
