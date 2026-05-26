using Amazon.SQS;
using Amazon.SQS.Model;
using GenerationService.Models;
using System.Text.Json;

namespace FileService.Services;

public class SqsListenerService(
    IAmazonSQS sqsClient,
    S3StorageService s3Service,
    ILogger<SqsListenerService> logger) : BackgroundService
{
    private const string QueueName = "software-projects-queue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS Listener started");

        var queueUrl = await GetOrCreateQueueUrlAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10,
                    MessageAttributeNames = ["All"]
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    logger.LogInformation("Получено сообщение из SQS. Длина: {Length}", message.Body.Length);

                    try
                    {
                        var contract = JsonSerializer.Deserialize<SoftwareProjectContract>(message.Body);
                        if (contract != null)
                        {
                            var key = $"software-project-contract-{contract.Id}.json";
                            await s3Service.SaveContractAsync(key, message.Body);
                            logger.LogInformation("✅ Обработан контракт {Id}", contract.Id);
                        }

                        await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка обработки сообщения");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в SQS Listener");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private async Task<string> GetOrCreateQueueUrlAsync()
    {
        try
        {
            var response = await sqsClient.GetQueueUrlAsync(QueueName);
            return response.QueueUrl;
        }
        catch
        {
            var createResponse = await sqsClient.CreateQueueAsync(QueueName);
            return createResponse.QueueUrl;
        }
    }
}