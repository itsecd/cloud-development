using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CourseManagement.Storage.Services;

/// <summary>
/// Сервис для манипуляции файлами в объектном хранилище
/// </summary>
/// /// <param name="logger">Логер</param>
/// <param name="client">S3 клиент</param>
/// <param name="configuration">Конфигурация</param>
public class S3AwsService(ILogger<S3AwsService> logger, IAmazonS3 client, IConfiguration configuration) : IS3Service
{
    /// <summary>
    /// Идентификатор бакета
    /// </summary>
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    /// <summary>
    /// Регион AWS
    /// </summary>
    private readonly string _region = configuration["AWS:Region"] 
        ?? throw new KeyNotFoundException("AWS region was not found in configuration");

    ///<inheritdoc/>
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData) ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = rootNode["id"]?.GetValue<int>() ?? throw new ArgumentException("Passed JSON has invalid structure");

        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, rootNode);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Began uploading {file} onto {Bucket}", id, _bucketName);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"course_{id}.json",
            InputStream = stream
        };

        var response = await client.PutObjectAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload {File}: {Code}", id, response.HttpStatusCode);
            return false;
        }

        logger.LogInformation("Finished uploading {File} to {Bucket}", id, _bucketName);
        
        return true;
    }

    ///<inheritdoc/>
    public async Task<List<string>> GetFileList()
    {
        var list = new List<string>();

        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = "",
            Delimiter = ",",
        };
        var paginator = client.Paginators.ListObjectsV2(request);

        logger.LogInformation("Began listing files in {Bucket}", _bucketName);

        await foreach (var response in paginator.Responses)
            if (response != null && response.S3Objects != null)
                foreach (var obj in response.S3Objects)
                {
                    if (obj != null)
                        list.Add(obj.Key);
                    else
                        logger.LogWarning("Received null object from {Bucket}", _bucketName);
                }
            else
                logger.LogWarning("Received null response from {Bucket}", _bucketName);

        return list;
    }

    ///<inheritdoc/>
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Began downloading {File} from {Bucket}", key, _bucketName);

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            using var response = await client.GetObjectAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Failed to download {File}: {Code}", key, response.HttpStatusCode);
                throw new InvalidOperationException($"Error occurred downloading {key} - {response.HttpStatusCode}");
            }
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);

            return JsonNode.Parse(reader.ReadToEnd()) ?? throw new InvalidOperationException($"Downloaded document is not a valid JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {File} downloading ", key);
            throw;
        }
    }

    ///<inheritdoc/>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking whether {Bucket} exists", _bucketName);

        try
        {
            var putBucketRequest = new PutBucketRequest
            {
                BucketName = _bucketName,
                BucketRegionName = _region
            };

            await client.PutBucketAsync(putBucketRequest);
            logger.LogInformation("{Bucket} existence ensured", _bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou" ||
                                               ex.ErrorCode == "BucketAlreadyExists" ||
                                               ex.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogInformation("{Bucket} already exists", _bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred during {Bucket} check", _bucketName);
            throw;
        }
    }
}
