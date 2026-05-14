using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace File.Service.Storage;

/// <summary>
/// Реализация сервиса хранения файлов транспортных средств в S3 (LocalStack)
/// </summary>
/// <param name="client">Клиент Amazon S3</param>
/// <param name="configuration">Конфигурация приложения (ключ <c>AWS:Resources:S3BucketName</c>)</param>
/// <param name="logger">Логгер</param>
public class S3VehicleStorageService(
    IAmazonS3 client,
    IConfiguration configuration,
    ILogger<S3VehicleStorageService> logger) : IVehicleStorageService
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    /// <inheritdoc />
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Ensuring bucket {Bucket} exists", _bucketName);
        await client.EnsureBucketExistsAsync(_bucketName);
    }

    /// <inheritdoc />
    public async Task<bool> Upload(string vehicleJson)
    {
        var rootNode = JsonNode.Parse(vehicleJson)
            ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = rootNode["systemId"]?.GetValue<int>()
            ?? rootNode["SystemId"]?.GetValue<int>()
            ?? throw new ArgumentException("Passed JSON has no systemId field");

        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, rootNode);
        stream.Seek(0, SeekOrigin.Begin);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = IVehicleStorageService.KeyFor(id),
            InputStream = stream
        };

        logger.LogInformation("Uploading vehicle {Id} to bucket {Bucket}", id, _bucketName);
        var response = await client.PutObjectAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload vehicle {Id}: {Code}", id, response.HttpStatusCode);
            return false;
        }
        return true;
    }

    /// <inheritdoc />
    public async Task<List<string>> ListKeys()
    {
        var list = new List<string>();
        var request = new ListObjectsV2Request { BucketName = _bucketName };
        var paginator = client.Paginators.ListObjectsV2(request);
        await foreach (var response in paginator.Responses)
        {
            if (response?.S3Objects == null) continue;
            foreach (var obj in response.S3Objects)
                if (obj?.Key != null)
                    list.Add(obj.Key);
        }
        return list;
    }

    /// <inheritdoc />
    public async Task<JsonNode?> Download(string key)
    {
        try
        {
            var request = new GetObjectRequest { BucketName = _bucketName, Key = key };
            using var response = await client.GetObjectAsync(request);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Failed to download {Key}: {Code}", key, response.HttpStatusCode);
                return null;
            }
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            return JsonNode.Parse(await reader.ReadToEndAsync());
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
