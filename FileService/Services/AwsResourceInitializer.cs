using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;

namespace FileService.Services;

public class AwsResourceInitializer(
    IAmazonSimpleNotificationService sns,
    IAmazonS3 s3,
    IAmazonSQS sqs,
    ILogger<AwsResourceInitializer> logger) : BackgroundService
{
    private const string TopicName = "software-projects-topic";
    private const string QueueName = "software-projects-queue";
    private const string BucketName = "software-projects";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeS3Async();
        await InitializeSnsAndSqsAsync();
        logger.LogInformation("✅ AWS ресурсы (S3 + SNS + SQS + Subscription) инициализированы");
    }

    private async Task InitializeS3Async()
    {
        try
        {
            await s3.PutBucketAsync(new PutBucketRequest { BucketName = BucketName });
            logger.LogInformation("S3 Bucket создан: {Bucket}", BucketName);
        }
        catch (Exception ex) when (ex.Message.Contains("BucketAlreadyExists") || ex.Message.Contains("AlreadyOwnedByYou"))
        {
            logger.LogInformation("S3 Bucket уже существует");
        }
    }

    private async Task InitializeSnsAndSqsAsync()
    {
        string? topicArn = null;
        string? queueUrl = null;

        try
        {
            // Создаём Topic
            var topicResponse = await sns.CreateTopicAsync(TopicName);
            topicArn = topicResponse.TopicArn;
            logger.LogInformation("SNS Topic создан: {Arn}", topicArn);

            // Создаём Queue
            var queueResponse = await sqs.CreateQueueAsync(QueueName);
            queueUrl = queueResponse.QueueUrl;
            logger.LogInformation("SQS Queue создан: {Url}", queueUrl);

            // Подписываем SQS на SNS
            await sns.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = "sqs",
                Endpoint = queueUrl
            });

            logger.LogInformation("✅ SQS подписан на SNS Topic");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при настройке SNS/SQS (возможно уже настроено)");
        }
    }
}