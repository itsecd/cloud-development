using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text;
using System.Text.Json.Nodes;

namespace CreditApp.FileService.Services;

/// <summary>
/// Служба для приёма сообщений из очереди SQS и сохранения в S3
/// </summary>
public class SqsConsumer(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsConsumer> logger) : BackgroundService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

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

            logger.LogInformation("Received {count} messages", response.Messages?.Count ?? 0);

            if (response.Messages != null)
            {
                foreach (var message in response.Messages)
                {
                    try
                    {
                        logger.LogInformation("Processing message: {messageId}", message.MessageId);

                        var rootNode = JsonNode.Parse(message.Body)
                            ?? throw new ArgumentException("Message body is not a valid JSON");
                        var id = rootNode["Id"]?.GetValue<int>()
                            ?? throw new ArgumentException("Message JSON has no Id field");

                        using var scope = scopeFactory.CreateScope();
                        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
                        var data = Encoding.UTF8.GetBytes(message.Body);
                        await storage.SaveAsync($"credit_{id}.json", data, stoppingToken);

                        _ = await sqsClient.DeleteMessageAsync(_queueName, message.ReceiptHandle, stoppingToken);
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