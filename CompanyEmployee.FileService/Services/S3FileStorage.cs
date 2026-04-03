using Amazon.S3;
using Amazon.S3.Model;

namespace CompanyEmployee.FileService.Services;

public class S3FileStorage : IS3FileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3FileStorage> _logger;

    public S3FileStorage(IAmazonS3 s3Client, ILogger<S3FileStorage> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
    }

    public async Task UploadFileAsync(string bucketName, string key, byte[] content)
    {
        try
        {
            var bucketExists = await _s3Client.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists)
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
            }

            using var stream = new MemoryStream(content);
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = "application/json"
            });

            _logger.LogInformation("Файл {Key} загружен в {BucketName}", key, bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки файла {Key}", key);
            throw;
        }
    }
}