using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Minio;
using Minio.DataModel.Args;

namespace Cloud.EventSink.S3;

/// <summary>
/// Служба, реализующая интерфейс IS3Service для Minio
/// </summary>
/// <param name="client">Клиент Minio</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public class S3Service(
    IMinioClient client,
    IConfiguration configuration,
    ILogger<S3Service> logger
    ) : IS3Service
{
    private readonly string _bucketName = configuration["AWS:Resources:MinioBucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name not found in configuration");

    /// <inheritdoc />
    public async Task<List<string>> GetFileList()
    {
        logger.LogInformation("Listing files in bucket {BucketName}", _bucketName);
        var request = new ListObjectsArgs().WithBucket(_bucketName).WithPrefix("").WithRecursive(true);
        var items = client.ListObjectsEnumAsync(request);
        var list = new List<string>();
        await foreach (var item in items)
            list.Add(item.Key);
        return list;
    }

    /// <inheritdoc />
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData) ?? throw new ArgumentException("Invalid JSON");
        var id = rootNode["id"]?.GetValue<int>() ?? throw new ArgumentException("JSON must contain 'id'");

        var bytes = Encoding.UTF8.GetBytes(fileData);
        using var stream = new MemoryStream(bytes);
        var putRequest = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithObject($"cloud_employee_{id}.json");

        logger.LogInformation("Uploading employee {Id} to bucket {BucketName}", id, _bucketName);
        var response = await client.PutObjectAsync(putRequest);
        if (response.ResponseStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Upload failed for employee {Id}, status {StatusCode}", id, response.ResponseStatusCode);
            return false;
        }
        logger.LogInformation("Successfully uploaded employee {Id}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<JsonNode> DownloadFile(string filePath)
    {
        logger.LogInformation("Downloading {FilePath} from {BucketName}", filePath, _bucketName);
        var memoryStream = new MemoryStream();
        var getRequest = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(filePath)
            .WithCallbackStream(async (stream, ct) =>
            {
                await stream.CopyToAsync(memoryStream, ct);
                memoryStream.Seek(0, SeekOrigin.Begin);
            });

        await client.GetObjectAsync(getRequest);
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var content = reader.ReadToEnd();
        return JsonNode.Parse(content) ?? throw new InvalidOperationException("Downloaded file is not valid JSON");
    }

    /// <inheritdoc />
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking bucket existence: {BucketName}", _bucketName);
        var existsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        var exists = await client.BucketExistsAsync(existsArgs);
        if (!exists)
        {
            logger.LogInformation("Creating bucket: {BucketName}", _bucketName);
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }
        else
        {
            logger.LogInformation("Bucket already exists: {BucketName}", _bucketName);
        }
    }
}