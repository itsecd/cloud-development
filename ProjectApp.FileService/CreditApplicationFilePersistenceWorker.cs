using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using ProjectApp.Domain.Messaging;
using ProjectApp.FileService.Options;

namespace ProjectApp.FileService;

public class CreditApplicationFilePersistenceWorker(
    IAmazonSQS sqsClient,
    IAmazonS3 s3Client,
    IOptions<FilePersistenceOptions> options,
    ILogger<CreditApplicationFilePersistenceWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string? _queueUrl;
    private bool _bucketReady;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _queueUrl = await EnsureQueueAsync(stoppingToken);
        await EnsureBucketAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 10
            }, stoppingToken);

            if (response.Messages.Count == 0)
            {
                continue;
            }

            foreach (var message in response.Messages)
            {
                await HandleMessageAsync(message, stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<CreditApplicationGeneratedEvent>(message.Body, JsonOptions);
            if (evt is null)
            {
                logger.LogWarning("Message {MessageId} cannot be deserialized and will be removed", message.MessageId);
                await DeleteMessageAsync(message, cancellationToken);
                return;
            }

            var key = $"credit-applications/{evt.Id}-{evt.OccurredAtUtc:yyyyMMddHHmmssfff}.json";
            var payload = JsonSerializer.Serialize(evt.Application, JsonOptions);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = options.Value.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = "application/json"
            }, cancellationToken);

            await DeleteMessageAsync(message, cancellationToken);
            logger.LogInformation("Saved credit application {Id} to object storage as {Key}", evt.Id, key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process SQS message {MessageId}", message.MessageId);
        }
    }

    private async Task<string> EnsureQueueAsync(CancellationToken cancellationToken)
    {
        var queueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = options.Value.QueueName
        }, cancellationToken);

        return queueResponse.QueueUrl;
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady)
        {
            return;
        }

        var buckets = await s3Client.ListBucketsAsync(cancellationToken);
        if (buckets.Buckets.All(b => b.BucketName != options.Value.BucketName))
        {
            await s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = options.Value.BucketName
            }, cancellationToken);
        }

        _bucketReady = true;
    }

    private async Task DeleteMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (_queueUrl is null)
        {
            return;
        }

        await sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, cancellationToken);
    }
}
