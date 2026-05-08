using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Minio;
using Minio.DataModel.Args;

namespace Cloud.EventSink.S3;

/// <summary>
/// Служба, реализующая интерфейс IS3Service для Minio
/// </summary>
public class S3Service : IS3Service
{
    private readonly string _bucketName;
    private readonly IMinioClient _client;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IMinioClient client, IConfiguration configuration, ILogger<S3Service> logger)
    {
        _client = client;
        _logger = logger;
        _bucketName = configuration["AWS:Resources:MinioBucketName"]
                      ?? throw new KeyNotFoundException("S3 bucket name not found in configuration");
    }

    /// <inheritdoc />
    public async Task<List<string>> GetFileList()
    {
        _logger.LogInformation("Listing files in bucket {BucketName}", _bucketName);
        var request = new ListObjectsArgs().WithBucket(_bucketName).WithPrefix("").WithRecursive(true);
        var items = _client.ListObjectsEnumAsync(request);
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

        _logger.LogInformation("Uploading employee {Id} to bucket {BucketName}", id, _bucketName);
        var response = await _client.PutObjectAsync(putRequest);
        if (response.ResponseStatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("Upload failed for employee {Id}, status {StatusCode}", id, response.ResponseStatusCode);
            return false;
        }
        _logger.LogInformation("Successfully uploaded employee {Id}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<JsonNode> DownloadFile(string filePath)
    {
        _logger.LogInformation("Downloading {FilePath} from {BucketName}", filePath, _bucketName);
        var memoryStream = new MemoryStream();
        var getRequest = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(filePath)
            .WithCallbackStream(async (stream, ct) =>
            {
                await stream.CopyToAsync(memoryStream, ct);
                memoryStream.Seek(0, SeekOrigin.Begin);
            });

        await _client.GetObjectAsync(getRequest);
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        var content = reader.ReadToEnd();
        return JsonNode.Parse(content) ?? throw new InvalidOperationException("Downloaded file is not valid JSON");
    }

    /// <inheritdoc />
    public async Task EnsureBucketExists()
    {
        _logger.LogInformation("Checking bucket existence: {BucketName}", _bucketName);
        var existsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        var exists = await _client.BucketExistsAsync(existsArgs);
        if (!exists)
        {
            _logger.LogInformation("Creating bucket: {BucketName}", _bucketName);
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }
        else
        {
            _logger.LogInformation("Bucket already exists: {BucketName}", _bucketName);
        }
    }
}