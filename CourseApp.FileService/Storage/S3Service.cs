using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CourseApp.Api.Models;

namespace CourseApp.FileService.Storage;

/// <summary>
/// Реализация IS3Service на AWS SDK для S3 (через LocalStack)
/// </summary>
/// <param name="client">Клиент S3</param>
/// <param name="configuration">Конфигурация (имя бакета берётся из CloudFormation Outputs)</param>
/// <param name="logger">Логгер</param>
public sealed class S3Service(
    IAmazonS3 client,
    IConfiguration configuration,
    ILogger<S3Service> logger) : IS3Service
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    /// <inheritdoc/>
    public async Task<bool> UploadFile(Course course)
    {
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, course);
        stream.Seek(0, SeekOrigin.Begin);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"course_{course.Id}.json",
            InputStream = stream,
            ContentType = "application/json"
        };

        var response = await client.PutObjectAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload course {Id}: {Code}", course.Id, response.HttpStatusCode);
            return false;
        }
        logger.LogInformation("Uploaded course {Id} to bucket {Bucket}", course.Id, _bucketName);
        return true;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetFileList()
    {
        var keys = new List<string>();
        var paginator = client.Paginators.ListObjectsV2(new ListObjectsV2Request { BucketName = _bucketName });
        await foreach (var response in paginator.Responses)
            if (response?.S3Objects is { } objects)
                keys.AddRange(objects.Where(o => o is not null).Select(o => o.Key));
        return keys;
    }

    /// <inheritdoc/>
    public async Task<Course> DownloadFile(string key)
    {
        using var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = _bucketName, Key = key });
        if (response.HttpStatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Error downloading {key}: {response.HttpStatusCode}");

        return await JsonSerializer.DeserializeAsync<Course>(response.ResponseStream)
            ?? throw new InvalidOperationException($"Object {key} is not a valid Course JSON");
    }

    /// <inheritdoc/>
    public async Task EnsureBucketExists()
    {
        await client.EnsureBucketExistsAsync(_bucketName);
        logger.LogInformation("Bucket {Bucket} existence ensured", _bucketName);
    }
}
