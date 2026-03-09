using Amazon.S3;
using Amazon.S3.Model;

namespace CreditApplication.FileService.Services;

/// <summary>
/// Обёртка над S3-клиентом для загрузки, получения и перечисления файлов.
/// </summary>
public class S3StorageService(
    IAmazonS3 s3Client,
    IConfiguration configuration,
    ILogger<S3StorageService> logger)
{
    private readonly string _bucketName = configuration["AWS:S3BucketName"] ?? "credit-applications";

    public async Task UploadAsync(string key, string jsonContent, CancellationToken ct = default)
    {
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            ContentBody = jsonContent,
            ContentType = "application/json"
        }, ct);

        logger.LogInformation("Uploaded '{Key}' to S3 bucket '{Bucket}'", key, _bucketName);
    }

    public async Task<IReadOnlyList<string>> ListFilesAsync(CancellationToken ct = default)
    {
        var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _bucketName
        }, ct);

        return (response.S3Objects ?? []).Select(o => o.Key).ToList();
    }

    public async Task<string?> GetFileAsync(string key, CancellationToken ct = default)
    {
        try
        {
            using var response = await s3Client.GetObjectAsync(_bucketName, key, ct);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync(ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
