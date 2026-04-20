using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace FileService.Services;

public sealed class MinioStorageService : IDisposable
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public MinioStorageService(IConfiguration configuration)
    {
        var serviceUrl = configuration["Minio:ServiceUrl"] ?? "http://localhost:9000";
        var accessKey = configuration["Minio:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["Minio:SecretKey"] ?? "minioadmin";
        _bucket = configuration["Minio:BucketName"] ?? "patients";

        _s3 = new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1"
            });
    }

    public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
    {
        try
        {
            await _s3.PutBucketAsync(_bucket, ct);
        }
        catch (AmazonS3Exception e) when (e.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
        }
    }

    public async Task SavePatientAsync(string patientJson, int patientId, CancellationToken ct = default)
    {
        var key = $"patient-{patientId}-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            ContentBody = patientJson,
            ContentType = "application/json"
        }, ct);
    }

    public async Task<List<string>> ListFilesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket
            }, ct);
            return response.S3Objects.Select(o => o.Key).ToList();
        }
        catch
        {
            return [];
        }
    }

    public void Dispose() => _s3.Dispose();
}
