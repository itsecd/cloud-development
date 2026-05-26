using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;

namespace FileService.Services;

public class AwsResourceInitializer(
    IAmazonSimpleNotificationService sns,
    IAmazonS3 s3,
    ILogger<AwsResourceInitializer> logger) : BackgroundService
{
    private const string TopicName = "software-projects-topic";
    private const string BucketName = "software-projects";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeS3BucketAsync();
        logger.LogInformation("AWS resources initialized successfully");
    }

    private async Task InitializeS3BucketAsync()
    {
        try
        {
            var request = new PutBucketRequest
            {
                BucketName = BucketName
            };
            await s3.PutBucketAsync(request);
            logger.LogInformation("Bucket created: {Bucket}", BucketName);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyExists" ||
                                           ex.ErrorCode == "BucketAlreadyOwnedByYou")
        {
            logger.LogInformation("Bucket already exists: {Bucket}", BucketName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create bucket {Bucket}", BucketName);
        }
    }
}