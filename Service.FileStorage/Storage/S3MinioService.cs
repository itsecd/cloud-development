using Minio;
using Minio.DataModel.Args;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Service.FileStorage.Storage;

/// <summary>
/// Служба для манипуляции файлами в объектном хранилище Minio
/// </summary>
/// <param name="client">Клиент Minio</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class S3MinioService(IMinioClient client, IConfiguration configuration, ILogger<S3MinioService> logger) : IS3Service
{
    /// <summary>
    /// Имя бакета в Minio
    /// </summary>
    private readonly string _bucketName = configuration["AWS:Resources:MinioBucketName"]
        ?? throw new KeyNotFoundException("Minio bucket name was not found in configuration");

    /// <inheritdoc/>
    public async Task<List<string>> GetFileList()
    {
        var list = new List<string>();
        var request = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithPrefix("")
            .WithRecursive(true);

        var responseList = client.ListObjectsEnumAsync(request);
        await foreach (var response in responseList)
            list.Add(response.Key);

        return list;
    }

    /// <inheritdoc/>
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData) ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = (rootNode["id"] ?? rootNode["Id"])?.GetValue<int>() ?? throw new ArgumentException("Passed JSON has invalid structure");

        var bytes = Encoding.UTF8.GetBytes(fileData);
        using var stream = new MemoryStream(bytes);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Uploading credit application {Id} to {Bucket}", id, _bucketName);
        var request = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithObject($"creditapp_{id}.json");

        var response = await client.PutObjectAsync(request);

        if (response.ResponseStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload credit application {Id}: {Code}", id, response.ResponseStatusCode);
            return false;
        }

        logger.LogInformation("Uploaded credit application {Id} to {Bucket}", id, _bucketName);
        return true;
    }

    /// <inheritdoc/>
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Downloading {File} from {Bucket}", key, _bucketName);
        var memoryStream = new MemoryStream();

        var request = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(key)
            .WithCallbackStream(async (stream, cancellationToken) =>
            {
                await stream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Seek(0, SeekOrigin.Begin);
            });

        var response = await client.GetObjectAsync(request) 
            ?? throw new InvalidOperationException($"Error downloading {key} — object is null");
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        return JsonNode.Parse(reader.ReadToEnd())
            ?? throw new InvalidOperationException("Downloaded document is not a valid JSON");
    }

    /// <inheritdoc/>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking whether {Bucket} exists", _bucketName);
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (!exists)
        {
            logger.LogInformation("Creating {Bucket}", _bucketName);
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }
    }
}
