using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditApplication.FileService.Services;

/// <summary>
/// Readiness-проверка: убеждается, что S3-бакет существует и доступен.
/// </summary>
public class S3HealthCheck(
    IAmazonS3 s3Client,
    IConfiguration configuration) : IHealthCheck
{
    private readonly string _bucketName =
        configuration["AWS:S3BucketName"] ?? "credit-applications";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                MaxKeys = 1
            }, cancellationToken);

            return HealthCheckResult.Healthy($"S3 bucket '{_bucketName}' is accessible");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return HealthCheckResult.Unhealthy($"S3 bucket '{_bucketName}' does not exist");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 is not reachable", ex);
        }
    }
}
