using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace File.Service.Storage;

/// <summary>
/// Реализация <see cref="IObjectStorage"/> поверх AWS-совместимого S3 (LocalStack)
/// </summary>
/// <param name="client">Клиент AWS SDK для S3, разрешённый из DI</param>
/// <param name="configuration">Конфигурация приложения; используется для получения имени бакета</param>
/// <param name="logger">Структурный логгер</param>
public class S3ObjectStorage(
    IAmazonS3 client,
    IConfiguration configuration,
    ILogger<S3ObjectStorage> logger) : IObjectStorage
{
    /// <summary>
    /// Имя S3-бакета. Берётся из CloudFormation outputs (<c>AWS:Resources:S3BucketName</c>)
    /// </summary>
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    /// <inheritdoc/>
    public async Task<bool> UploadProject(string payload)
    {
        var rootNode = JsonNode.Parse(payload) ?? throw new ArgumentException("Payload is not a valid JSON");
        var id = rootNode["Id"]?.GetValue<int>()
            ?? rootNode["id"]?.GetValue<int>()
            ?? throw new ArgumentException("Payload JSON does not contain 'Id' field");

        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, rootNode);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Uploading software project {ProjectId} to bucket {Bucket}", id, _bucketName);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"software-project-{id}.json",
            InputStream = stream,
            ContentType = "application/json"
        };

        var response = await client.PutObjectAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload software project {ProjectId}: {Code}", id, response.HttpStatusCode);
            return false;
        }

        logger.LogInformation("Software project {ProjectId} successfully uploaded to bucket {Bucket}", id, _bucketName);
        return true;
    }

    /// <inheritdoc/>
    public async Task<List<string>> ListProjects()
    {
        var keys = new List<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = string.Empty
        };

        logger.LogInformation("Listing objects in bucket {Bucket}", _bucketName);
        var paginator = client.Paginators.ListObjectsV2(request);
        await foreach (var response in paginator.Responses)
        {
            if (response?.S3Objects is null)
            {
                logger.LogWarning("Received empty response page from bucket {Bucket}", _bucketName);
                continue;
            }

            foreach (var obj in response.S3Objects)
                if (obj is not null)
                    keys.Add(obj.Key);
        }

        return keys;
    }

    /// <inheritdoc/>
    public async Task<JsonNode> DownloadProject(string key)
    {
        logger.LogInformation("Downloading object {Key} from bucket {Bucket}", key, _bucketName);

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        using var response = await client.GetObjectAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to download {Key} from bucket {Bucket}: {Code}", key, _bucketName, response.HttpStatusCode);
            throw new InvalidOperationException($"Error occurred downloading {key} - {response.HttpStatusCode}");
        }

        using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        return JsonNode.Parse(content)
            ?? throw new InvalidOperationException($"Object {key} contents are not a valid JSON");
    }

    /// <inheritdoc/>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Ensuring bucket {Bucket} exists", _bucketName);
        try
        {
            await client.EnsureBucketExistsAsync(_bucketName);
            logger.LogInformation("Bucket {Bucket} is ready", _bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while ensuring bucket {Bucket}", _bucketName);
            throw;
        }
    }
}
