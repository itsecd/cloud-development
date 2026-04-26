using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inventory.FileService.Storage;

/// <summary>
/// Cлужба для манипуляции файлами в объектном хранилище
/// </summary>
/// <param name="client"> S3 клиент</param>
/// <param name="configuration"> Конфигурация</param>
/// <param name="logger"> Логер</param>
public class S3AwsService(IAmazonS3 client, IConfiguration configuration, ILogger<S3AwsService> logger) : IS3Service
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

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

        logger.LogInformation("Began listing files in {bucket}", _bucketName);

        await foreach (var response in paginator.Responses)
        {
            if (response != null && response.S3Objects != null)
            {
                foreach (var obj in response.S3Objects)
                {
                    if (obj != null)
                        list.Add(obj.Key);
                    else
                        logger.LogWarning("Received null object from {bucket}", _bucketName);
                }
            }
            else
            {
                logger.LogWarning("Received null response from {bucket}", _bucketName);
            }
        }

        return list;
    }

    ///<inheritdoc/>
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData)
            ?? throw new ArgumentException("Passed string is not a valid JSON");

        var idNode = rootNode["id"] ?? rootNode["Id"];

        if (idNode is null)
        {
            logger.LogError("SNS message JSON has no id/Id field. Payload: {Payload}", fileData);
            throw new ArgumentException("Passed JSON has invalid structure");
        }

        var id = idNode.GetValue<int>();

        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, rootNode);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Began uploading inventory {file} onto {bucket}", id, _bucketName);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = $"inventory_{id}.json",
            InputStream = stream,
            ContentType = "application/json"
        };

        var response = await client.PutObjectAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload inventory {file}: {code}", id, response.HttpStatusCode);
            return false;
        }

        logger.LogInformation("Finished uploading inventory {file} to {bucket}", id, _bucketName);
        return true;
    }

    ///<inheritdoc/>
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Began downloading {file} from {bucket}", key, _bucketName);

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
                logger.LogError("Failed to download {file}: {code}", key, response.HttpStatusCode);
                throw new InvalidOperationException($"Error occurred downloading {key} - {response.HttpStatusCode}");
            }

            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            return JsonNode.Parse(content)
                ?? throw new InvalidOperationException("Downloaded document is not a valid JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during {file} downloading", key);
            throw;
        }
    }

    ///<inheritdoc/>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking whether {bucket} exists", _bucketName);

        try
        {
            await client.GetBucketLocationAsync(new GetBucketLocationRequest
            {
                BucketName = _bucketName
            });

            logger.LogInformation("{bucket} already exists", _bucketName);
            return;
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == HttpStatusCode.NotFound ||
            ex.ErrorCode == "NoSuchBucket" ||
            ex.ErrorCode == "NotFound")
        {
            logger.LogInformation("{bucket} does not exist, creating it", _bucketName);
        }

        var region = configuration["AWS:Region"]
            ?? configuration["AWS_REGION"]
            ?? configuration["AWS_DEFAULT_REGION"]
            ?? "eu-central-1";

        var request = new PutBucketRequest{
            BucketName = _bucketName
        };

        if (!string.Equals(region, "us-east-1", StringComparison.OrdinalIgnoreCase))
            request.BucketRegionName = region;

        try
        {
            await client.PutBucketAsync(request);
            logger.LogInformation("{bucket} created in region {region}", _bucketName, region);
        }
        catch (AmazonS3Exception ex) when (
            ex.ErrorCode == "BucketAlreadyOwnedByYou" ||
            ex.ErrorCode == "BucketAlreadyExists" ||
            ex.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogInformation("{bucket} already exists", _bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred during {bucket} check", _bucketName);
            throw;
        }
    }
}