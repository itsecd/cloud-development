using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

namespace CreditApplication.FileService.Services;

/// <summary>
/// Фоновый сервис, опрашивающий SQS-очередь.
/// Извлекает JSON кредитной заявки из SNS-конверта и сохраняет в S3.
/// </summary>
public class SqsListenerService(
    IAmazonSQS sqsClient,
    S3StorageService storageService,
    IConfiguration configuration,
    ILogger<SqsListenerService> logger) : BackgroundService
{
    private readonly string _queueName = configuration["AWS:SqsQueueName"] ?? "credit-applications-file-queue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = await ResolveQueueUrlAsync(stoppingToken);
        logger.LogInformation("SQS listener started, polling {QueueUrl}", queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                }, stoppingToken);

                foreach (var message in response.Messages ?? [])
                    await ProcessMessageAsync(queueUrl, message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error polling SQS queue");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string queueUrl, Message message, CancellationToken ct)
    {
        try
        {
            using var envelope = JsonDocument.Parse(message.Body);
            var payload = envelope.RootElement.GetProperty("Message").GetString();

            if (string.IsNullOrEmpty(payload))
            {
                logger.LogWarning("Empty message payload, skipping");
                return;
            }

            using var doc = JsonDocument.Parse(payload);
            var id = doc.RootElement.GetProperty("Id").GetInt32();
            var key = $"credit-application-{id}.json";

            await storageService.UploadAsync(key, payload, ct);

            await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, ct);
            logger.LogInformation("Processed credit application {Id}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing SQS message {MessageId}", message.MessageId);
        }
    }

    private async Task<string> ResolveQueueUrlAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await sqsClient.GetQueueUrlAsync(_queueName, ct);
                return response.QueueUrl;
            }
            catch
            {
                logger.LogWarning("Queue '{QueueName}' not found yet, retrying...", _queueName);
                await Task.Delay(2000, ct);
            }
        }

        throw new OperationCanceledException(ct);
    }
}
