using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;
using System.Text;
using System.Text.Json;

namespace File.Service.Storage;

/// <summary>
/// Реализация сервиса для работы с хранилищем S3
/// </summary>
public class S3VehicleStorageService(
    IAmazonS3 s3Client,
    IAmazonSQS sqsClient,
    IConfiguration configuration,
    ILogger<S3VehicleStorageService> logger) : IVehicleStorageService
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new InvalidOperationException("S3 bucket name is not configured");
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new InvalidOperationException("SQS queue name is not configured");
    private string? _queueUrl;

    public async Task PrepareBucketAsync()
    {
        try
        {
            var request = new PutBucketRequest
            {
                BucketName = _bucketName,
                UseClientRegion = true
            };
            await s3Client.PutBucketAsync(request);
            logger.LogInformation("Bucket {BucketName} created", _bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogInformation("Bucket {BucketName} already exists", _bucketName);
        }
    }

    public async Task PrepareQueueAsync()
    {
        try
        {
            var request = new CreateQueueRequest
            {
                QueueName = _queueName
            };

            var response = await sqsClient.CreateQueueAsync(request);

            _queueUrl = response.QueueUrl;

            logger.LogInformation("Queue {QueueName} created with URL {QueueUrl}",
                _queueName, _queueUrl);
        }
        catch (AmazonSQSException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogInformation("Queue {QueueName} already exists", _queueName);

            var response = await sqsClient.GetQueueUrlAsync(_queueName);
            _queueUrl = response.QueueUrl;

            logger.LogInformation("Queue resolved: {QueueUrl}", _queueUrl);
        }
    }

    public async Task<bool> StoreVehicleDataAsync(string jsonData)
    {
        try
        {
            logger.LogInformation("RAW JSON: {Json}", jsonData);

            using var document = JsonDocument.Parse(jsonData);
            var root = document.RootElement;

            if (!root.TryGetProperty("Id", out var idProp) &&
                !root.TryGetProperty("id", out idProp))
            {
                logger.LogError("JSON does not contain Id field: {Json}", jsonData);
                return false;
            }

            var vehicleId = idProp.GetInt32();

            var fileKey = $"car_{vehicleId}.json";

            logger.LogInformation("VehicleId: {Id}, FileKey: {Key}", vehicleId, fileKey);

            var bytes = Encoding.UTF8.GetBytes(jsonData);

            using var stream = new MemoryStream(bytes);

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                InputStream = stream,
                ContentType = "application/json"
            };

            var response = await s3Client.PutObjectAsync(putRequest);

            logger.LogInformation("S3 response: {Status}", response.HttpStatusCode);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("Vehicle {Id} uploaded successfully to {Key}", vehicleId, fileKey);
                return true;
            }

            logger.LogError("Failed upload. Status: {Status}", response.HttpStatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error storing vehicle data");
            return false;
        }
    }

    public async Task<List<string>> GetAllFileKeysAsync()
    {
        var keys = new List<string>();

        var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = _bucketName
        });

        foreach (var obj in response.S3Objects ?? [])
        {
            if (!string.IsNullOrEmpty(obj.Key))
                keys.Add(obj.Key);
        }

        return keys;
    }

    public async Task<JsonDocument?> FetchVehicleFileAsync(string fileKey)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            using var response = await s3Client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);

            var json = await reader.ReadToEndAsync();

            return JsonDocument.Parse(json);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("File not found: {Key}", fileKey);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching file {Key}", fileKey);
            return null;
        }
    }
}