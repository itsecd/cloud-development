using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Minio;
using Minio.DataModel.Args;

namespace File.Service.Storage;

/// <summary>
/// Реализация <see cref="IS3Service"/> на клиенте Minio.
/// Имя бакета берётся из ключа <c>AWS:Resources:MinioBucketName</c>
/// </summary>
/// <param name="client">Клиент Minio</param>
/// <param name="configuration">Источник имени бакета</param>
/// <param name="logger">Логгер операций с бакетом</param>
public class MinioS3Service(IMinioClient client, IConfiguration configuration, ILogger<MinioS3Service> logger) : IS3Service
{
    private readonly string _bucketName = configuration["AWS:Resources:MinioBucketName"]
        ?? throw new KeyNotFoundException("Minio bucket name was not found in configuration");

    /// <inheritdoc />
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking whether bucket {bucket} exists", _bucketName);
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (exists)
        {
            logger.LogInformation("Bucket {bucket} already exists", _bucketName);
            return;
        }
        logger.LogInformation("Creating bucket {bucket}", _bucketName);
        await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
    }

    /// <inheritdoc />
    public async Task<bool> UploadFile(string fileData)
    {
        var root = JsonNode.Parse(fileData) ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = root["Id"]?.GetValue<int>()
              ?? root["id"]?.GetValue<int>()
              ?? throw new ArgumentException("Passed JSON has no 'Id' property");

        var key = $"vehicle_{id}.json";
        var bytes = Encoding.UTF8.GetBytes(fileData);
        using var stream = new MemoryStream(bytes);

        logger.LogInformation("Uploading {key} to bucket {bucket}", key, _bucketName);
        var request = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(key)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length);
        var response = await client.PutObjectAsync(request);

        if (response.ResponseStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload {key}: status {code}", key, response.ResponseStatusCode);
            return false;
        }
        logger.LogInformation("Successfully uploaded {key}", key);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetFileList()
    {
        var keys = new List<string>();
        var request = new ListObjectsArgs().WithBucket(_bucketName).WithRecursive(true);
        logger.LogInformation("Listing objects in bucket {bucket}", _bucketName);
        await foreach (var item in client.ListObjectsEnumAsync(request))
            keys.Add(item.Key);
        return keys;
    }

    /// <inheritdoc />
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Downloading {key} from bucket {bucket}", key, _bucketName);
        var memory = new MemoryStream();
        var request = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(key)
            .WithCallbackStream(async (s, ct) =>
            {
                await s.CopyToAsync(memory, ct);
                memory.Seek(0, SeekOrigin.Begin);
            });
        await client.GetObjectAsync(request);
        using var reader = new StreamReader(memory, Encoding.UTF8);
        return JsonNode.Parse(reader.ReadToEnd())
            ?? throw new InvalidOperationException($"Downloaded {key} is not a valid JSON");
    }
}
