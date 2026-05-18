using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Minio;
using Minio.DataModel.Args;

namespace File.Service.Storage;

/// <summary>
/// Реализация файлового хранилища
/// </summary>
/// <param name="client">MinIO-клиент</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public sealed class MinioFileStorage(IMinioClient client, IConfiguration configuration, ILogger<MinioFileStorage> logger) : IFileStorage
{
    private readonly string _bucketName = configuration["AWS:Resources:MinioBucketName"]
        ?? throw new KeyNotFoundException("Minio bucket name was not found in configuration");

    /// <inheritdoc />
    public async Task<bool> UploadAsync(string payload)
    {
        var rootNode = JsonNode.Parse(payload) ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = rootNode["id"]?.GetValue<int>() ?? throw new ArgumentException("Passed JSON has invalid structure");

        var bytes = Encoding.UTF8.GetBytes(payload);
        using var stream = new MemoryStream(bytes);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Uploading vehicle {id} to bucket {bucket}", id, _bucketName);
        var request = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithObject($"vehicle_{id}.json");

        var response = await client.PutObjectAsync(request);
        if (response.ResponseStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload vehicle {id}: {code}", id, response.ResponseStatusCode);
            return false;
        }
        logger.LogInformation("Uploaded vehicle {id} to {bucket}", id, _bucketName);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<string>> ListAsync()
    {
        logger.LogInformation("Listing files in {bucket}", _bucketName);
        var request = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithPrefix("")
            .WithRecursive(true);

        var result = new List<string>();
        await foreach (var item in client.ListObjectsEnumAsync(request))
            result.Add(item.Key);
        return result;
    }

    /// <inheritdoc />
    public async Task<JsonNode> DownloadAsync(string key)
    {
        logger.LogInformation("Downloading {file} from {bucket}", key, _bucketName);
        var memoryStream = new MemoryStream();
        var request = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(key)
            .WithCallbackStream(async (stream, cancellationToken) =>
            {
                await stream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Seek(0, SeekOrigin.Begin);
            });

        var response = await client.GetObjectAsync(request) ?? throw new InvalidOperationException($"Failed to download {key}");
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        return JsonNode.Parse(content) ?? throw new InvalidOperationException("Downloaded document is not a valid JSON");
    }

    /// <inheritdoc />
    public async Task EnsureBucketExistsAsync()
    {
        logger.LogInformation("Checking whether {bucket} exists", _bucketName);
        var existsRequest = new BucketExistsArgs().WithBucket(_bucketName);
        var exists = await client.BucketExistsAsync(existsRequest);
        if (exists)
        {
            logger.LogInformation("Bucket {bucket} already exists", _bucketName);
            return;
        }
        var makeRequest = new MakeBucketArgs().WithBucket(_bucketName);
        await client.MakeBucketAsync(makeRequest);
        logger.LogInformation("Created bucket {bucket}", _bucketName);
    }
}
