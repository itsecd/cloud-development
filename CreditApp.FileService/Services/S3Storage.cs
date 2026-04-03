using Amazon.S3;
using Amazon.S3.Model;

using CreditApp.FileService;

/// <summary>
/// Хранилище файлов в S3 (LocalStack)
/// </summary>
public class S3Storage : IFileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3Storage> _logger;

    public S3Storage(IAmazonS3 s3Client, ILogger<S3Storage> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
    }

    /// <summary>
    /// Сохраняет файл в S3 bucket
    /// </summary>
    public async Task SaveAsync(string bucketName, string fileName, byte[] data, CancellationToken cancellationToken = default)
    {
        try
        {
            var buckets = await _s3Client.ListBucketsAsync(cancellationToken);
            if (!buckets.Buckets.Any(b => b.BucketName == bucketName))
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }, cancellationToken);
                _logger.LogInformation("Bucket {BucketName} created", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking/creating bucket");
        }

        using var stream = new MemoryStream(data);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = "application/json",
            UseChunkEncoding = false,
            AutoCloseStream = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        _logger.LogInformation("Saved {FileName} to S3 bucket {BucketName}", fileName, bucketName);
    }

    /// <summary>
    /// Получает файл из S3 bucket
    /// </summary>
    public async Task<byte[]?> GetAsync(string bucketName, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectRequest { BucketName = bucketName, Key = fileName };
            using var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            return ms.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}