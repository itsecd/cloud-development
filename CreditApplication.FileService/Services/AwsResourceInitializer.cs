using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace CreditApplication.FileService.Services;

/// <summary>
/// Создаёт необходимые AWS-ресурсы в LocalStack при старте приложения:
/// S3-бакет, SNS-топик, SQS-очередь и подписку очереди на топик.
/// </summary>
public class AwsResourceInitializer(
    IAmazonS3 s3Client,
    IAmazonSQS sqsClient,
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<AwsResourceInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucketName = configuration["AWS:S3BucketName"] ?? "credit-applications";
        var topicName = configuration["AWS:SnsTopicName"] ?? "credit-applications";
        var queueName = configuration["AWS:SqsQueueName"] ?? "credit-applications-file-queue";

        try
        {
            await s3Client.PutBucketAsync(bucketName, cancellationToken);
            logger.LogInformation("S3 bucket '{BucketName}' created", bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            logger.LogInformation("S3 bucket '{BucketName}' already exists", bucketName);
        }

        // SNS topic
        var topicResponse = await snsClient.CreateTopicAsync(topicName, cancellationToken);
        logger.LogInformation("SNS topic '{TopicName}' ready: {TopicArn}", topicName, topicResponse.TopicArn);

        // SQS queue
        var queueResponse = await sqsClient.CreateQueueAsync(queueName, cancellationToken);
        logger.LogInformation("SQS queue '{QueueName}' ready: {QueueUrl}", queueName, queueResponse.QueueUrl);

        // Get queue ARN for subscription
        var attrsResponse = await sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queueResponse.QueueUrl,
            AttributeNames = ["QueueArn"]
        }, cancellationToken);
        var queueArn = attrsResponse.Attributes["QueueArn"];

        // Subscribe SQS → SNS
        await snsClient.SubscribeAsync(topicResponse.TopicArn, "sqs", queueArn, cancellationToken);
        logger.LogInformation("SQS queue subscribed to SNS topic");

        // Set SQS policy to allow SNS to publish messages
        var policy = $$"""
        {
          "Version": "2012-10-17",
          "Id": "Policy{{Guid.NewGuid()}}",
          "Statement": [
            {
              "Sid": "Allow-SNS-SendMessage",
              "Effect": "Allow",
              "Principal": "*",
              "Action": "sqs:SendMessage",
              "Resource": "{{queueArn}}",
              "Condition": {
                "ArnEquals": {
                  "aws:SourceArn": "{{topicResponse.TopicArn}}"
                }
              }
            }
          ]
        }
        """;

        await sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = queueResponse.QueueUrl,
            Attributes = new Dictionary<string, string>
            {
                { "Policy", policy }
            }
        }, cancellationToken);
        logger.LogInformation("Set SQS policy to allow SNS topic to SendMessage");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
