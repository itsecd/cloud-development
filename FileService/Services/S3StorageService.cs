using Amazon.S3;
using Amazon.S3.Model;

namespace FileService.Services;

public class S3StorageService(IAmazonS3 s3Client, ILogger<S3StorageService> logger)
{
    private const string BucketName = "software-projects";

    public async Task SaveContractAsync(string key, string jsonContent)
    {
        try
        {
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                ContentBody = jsonContent,
                ContentType = "application/json"
            });

            logger.LogInformation("✅ Сохранено в S3: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Ошибка сохранения {Key} в S3", key);
        }
    }

    public async Task<List<string>> ListFilesAsync()
    {
        try
        {
            var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName
            });
            return response.S3Objects.Select(o => o.Key).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка получения списка файлов из S3");
            return new List<string>();
        }
    }

    public async Task<string?> GetFileAsync(string key)
    {
        try
        {
            var response = await s3Client.GetObjectAsync(BucketName, key);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync();
        }
        catch
        {
            return null;
        }
    }
}