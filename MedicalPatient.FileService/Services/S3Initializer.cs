using Amazon.S3;
using Amazon.S3.Model;

namespace MedicalPatient.FileService.Services;

public class S3Initializer(
    IAmazonS3 s3Client,
    ILogger<S3Initializer> logger,
    string bucketName) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var bucketExists = await BucketExistsAsync(bucketName, stoppingToken);

                if (!bucketExists)
                {
                    await s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = bucketName
                    }, stoppingToken);
                    logger.LogInformation("S3 bucket '{BucketName}' created successfully", bucketName);
                }
                else
                {
                    logger.LogInformation("S3 bucket '{BucketName}' already exists", bucketName);
                }

                return;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                logger.LogInformation("S3 bucket '{BucketName}' already exists", bucketName);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to initialize S3 bucket, retrying in 3 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            var request = new ListBucketsRequest();
            var response = await s3Client.ListBucketsAsync(request, cancellationToken);
            return response.Buckets.Any(b => b.BucketName == bucketName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking bucket existence");
            return false;
        }
    }
}