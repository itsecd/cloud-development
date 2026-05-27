using Amazon.S3;
using Amazon.S3.Model;
using ContractGenerator.Shared.Storage;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ContractGenerator.FileService.Storage;

/// <summary>
/// S3-реализация хранилища файлов сотрудников для LocalStack.
/// </summary>
/// <param name="client">Клиент Amazon S3.</param>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="logger">Логгер.</param>
public class S3EmployeeFileStorage(
    IAmazonS3 client,
    IConfiguration configuration,
    ILogger<S3EmployeeFileStorage> logger) : IEmployeeFileStorage
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    /// <inheritdoc />
    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Ensuring S3 bucket {BucketName} exists", _bucketName);
        await client.EnsureBucketExistsAsync(_bucketName);
    }

    /// <inheritdoc />
    public async Task SaveEmployeeJsonAsync(string employeeJson, CancellationToken cancellationToken = default)
    {
        var rootNode = JsonNode.Parse(employeeJson)
            ?? throw new ArgumentException("Employee message body is not a valid JSON", nameof(employeeJson));
        var id = rootNode["id"]?.GetValue<int>()
            ?? rootNode["Id"]?.GetValue<int>()
            ?? throw new ArgumentException("Employee message has no id field", nameof(employeeJson));

        var key = EmployeeFileKeys.ForId(id);
        var normalizedJson = rootNode.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(normalizedJson));
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json",
            UseChunkEncoding = false,
            AutoCloseStream = false
        };

        var response = await client.PutObjectAsync(request, cancellationToken);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"S3 returned {response.HttpStatusCode} while saving {key}");
        }

        logger.LogInformation("Saved employee {EmployeeId} to S3 object {Key}", id, key);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListKeysAsync(CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        var paginator = client.Paginators.ListObjectsV2(new ListObjectsV2Request
        {
            BucketName = _bucketName
        });

        await foreach (var response in paginator.Responses.WithCancellation(cancellationToken))
        {
            if (response.S3Objects is null)
            {
                continue;
            }

            keys.AddRange(response.S3Objects.Where(s3Object => s3Object.Key is not null).Select(s3Object => s3Object.Key));
        }

        return keys;
    }

    /// <inheritdoc />
    public async Task<JsonNode?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await client.GetObjectAsync(_bucketName, key, cancellationToken);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Failed to download S3 object {Key}: {StatusCode}", key, response.HttpStatusCode);
                return null;
            }

            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            return JsonNode.Parse(await reader.ReadToEndAsync(cancellationToken));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
