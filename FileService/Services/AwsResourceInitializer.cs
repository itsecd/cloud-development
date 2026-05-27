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
        try
        {
            var topicResponse = await sns.CreateTopicAsync(TopicName);
            var topicArn = topicResponse.TopicArn;
            logger.LogInformation("SNS Topic создан: {Arn}", topicArn);

            var queueResponse = await sqs.CreateQueueAsync(QueueName);
            var queueUrl = queueResponse.QueueUrl;
            logger.LogInformation("SQS Queue создан: {Url}", queueUrl);

            // Получаем ARN очереди для подписки
            var attrs = await sqs.GetQueueAttributesAsync(
                new Amazon.SQS.Model.GetQueueAttributesRequest
                {
                    QueueUrl = queueUrl,
                    AttributeNames = ["QueueArn"]
                });
            var queueArn = attrs.QueueARN;

            await sns.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = "sqs",
                Endpoint = queueArn  // ARN, не URL
            });

            logger.LogInformation("✅ SQS подписан на SNS. QueueArn={Arn}", queueArn);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при настройке SNS/SQS");
        }
    }
}