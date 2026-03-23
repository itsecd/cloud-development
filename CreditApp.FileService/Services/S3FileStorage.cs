using Amazon.S3;
using Amazon.S3.Model;

namespace CreditApp.FileService.Services;

/// <summary>
/// Реализация файлового хранилища на базе Amazon S3.
/// Обеспечивает загрузку JSON-файлов в указанный бакет.
/// </summary>
/// <param name="s3Client">Клиент Amazon S3.</param>
/// <param name="configuration">Конфигурация приложения для получения имени бакета.</param>
/// <param name="logger">Логгер для записи событий.</param>
public class S3FileStorage(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3FileStorage> logger) : IS3FileStorage
{
    private readonly string _bucketName = configuration["Aws:BucketName"] ?? "credit-files";

    /// <inheritdoc />
    public async Task UploadAsync(string key, string content, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync();

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            ContentBody = content,
            ContentType = "application/json"
        };

        await s3Client.PutObjectAsync(request, ct);
        logger.LogInformation("Uploaded file {Key} to bucket {Bucket}", key, _bucketName);
    }

    /// <summary>
    /// Проверяет существование бакета и создаёт его при необходимости.
    /// </summary>
    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            await s3Client.EnsureBucketExistsAsync(_bucketName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to ensure bucket {Bucket} exists", _bucketName);
        }
    }
}
