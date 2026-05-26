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
        var queueUrl = await GetOrCreateQueueUrlAsync();

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

                foreach (var message in response.Messages)
                {
                    try
                    {
                        var contract = JsonSerializer.Deserialize<GenerationService.Models.SoftwareProjectContract>(message.Body);
                        if (contract != null)
                        {
                            var key = $"software-project-contract-{contract.Id}.json";
                            await s3Service.SaveContractAsync(key, message.Body);
                        }

                        await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process message");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SQS listener");
                await Task.Delay(5000, stoppingToken);
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