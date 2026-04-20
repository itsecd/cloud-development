using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json.Nodes;

namespace CreditApp.FileService.Services;

/// <summary>
/// Служба для работы с файловым хранилищем S3
/// </summary>
public class S3Storage(IAmazonS3 client, IConfiguration configuration, ILogger<S3Storage> logger) : IFileStorage
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    public async Task SaveAsync(string fileName, byte[] data, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(data);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = "application/json",
            UseChunkEncoding = false,
            AutoCloseStream = true
        };

        await client.PutObjectAsync(request, cancellationToken);
        logger.LogInformation("Saved {FileName} to S3 bucket {BucketName}", fileName, _bucketName);
    }

    public async Task<List<string>> GetFileListAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.ListObjectsV2Async(
            new ListObjectsV2Request { BucketName = _bucketName },
            cancellationToken);
        return response.S3Objects?.Select(o => o.Key).ToList() ?? [];
    }

    public async Task<JsonNode> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetObjectAsync(_bucketName, key, cancellationToken);
        using var reader = new StreamReader(response.ResponseStream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        return JsonNode.Parse(content)
            ?? throw new InvalidOperationException($"File {key} is not a valid JSON");
    }

    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking whether {bucket} exists", _bucketName);
        await client.EnsureBucketExistsAsync(_bucketName);
        logger.LogInformation("{bucket} existence ensured", _bucketName);
    }
}
