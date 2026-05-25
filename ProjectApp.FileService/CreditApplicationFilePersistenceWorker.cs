using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using ProjectApp.Domain.Events;

namespace ProjectApp.FileService;

public class CreditApplicationFilePersistenceWorker(
    IAmazonSQS sqs,
    IAmazonS3 s3,
    IConfiguration configuration,
    ILogger<CreditApplicationFilePersistenceWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string? _queueUrl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bucketName = configuration["Minio:BucketName"] ?? "credit-applications";
        await RetryUntilReadyAsync(() => EnsureBucketExistsAsync(bucketName, stoppingToken), stoppingToken);
        _queueUrl = await RetryUntilReadyAsync(() => EnsureQueueExistsAsync(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 5
            }, stoppingToken);

            foreach (var message in response.Messages ?? [])
            {
                await ProcessMessageAsync(bucketName, message, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string bucketName, Message message, CancellationToken cancellationToken)
    {
        var generatedEvent = JsonSerializer.Deserialize<CreditApplicationGeneratedEvent>(message.Body, JsonOptions);
        if (generatedEvent?.Application is null)
        {
            logger.LogWarning("Received invalid SQS message {MessageId}", message.MessageId);
            return;
        }

        var key = $"credit-applications/{generatedEvent.Id}-{generatedEvent.OccurredAtUtc:yyyyMMddHHmmssfff}.json";
        var body = JsonSerializer.Serialize(generatedEvent.Application, JsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json"
        }, cancellationToken);

        await sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, cancellationToken);
        logger.LogInformation("Credit application {Id} saved to Minio as {Key}", generatedEvent.Id, key);
    }

    private async Task<string> EnsureQueueExistsAsync(CancellationToken cancellationToken)
    {
        var queueName = configuration["Sqs:QueueName"] ?? "credit-application-generated";
        var response = await sqs.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        }, cancellationToken);

        return response.QueueUrl;
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            await s3.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
        }
    }

    private async Task RetryUntilReadyAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await RetryUntilReadyAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);
    }

    private async Task<T> RetryUntilReadyAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested && attempt < 30)
            {
                attempt++;
                logger.LogWarning(ex, "External dependency is not ready yet, retrying attempt {Attempt}", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
