using Amazon.S3;
using Amazon.S3.Model;
using System.Net;
using System.Text;
using System.Text.Json;

namespace File.Service.Storage;

public class S3VehicleStorageService : IVehicleStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3VehicleStorageService> _logger;

    public S3VehicleStorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3VehicleStorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:Resources:S3BucketName"]
            ?? throw new InvalidOperationException("S3 bucket name is not configured");
        _logger = logger;
    }

    public async Task PrepareBucketAsync()
    {
        try
        {
            var request = new PutBucketRequest
            {
                BucketName = _bucketName,
                UseClientRegion = true
            };
            await _s3Client.PutBucketAsync(request);
            _logger.LogInformation("Bucket {BucketName} created", _bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogInformation("Bucket {BucketName} already exists", _bucketName);
        }
    }

    public async Task<bool> StoreVehicleDataAsync(string jsonData)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonData);
            var root = document.RootElement;

            var vehicleId = 0;
            if (root.TryGetProperty("Id", out var idProp))
                vehicleId = idProp.GetInt32();
            else if (root.TryGetProperty("id", out var idProp2))
                vehicleId = idProp2.GetInt32();
            else
                throw new InvalidOperationException("JSON does not contain Id field");

            var fileKey = IVehicleStorageService.BuildFileKey(vehicleId);
            var bytes = Encoding.UTF8.GetBytes(jsonData);

            using var stream = new MemoryStream(bytes);
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                InputStream = stream
            };

            var response = await _s3Client.PutObjectAsync(putRequest);
            var success = response.HttpStatusCode == HttpStatusCode.OK;

            if (success)
                _logger.LogInformation("Vehicle {Id} uploaded to {Key}", vehicleId, fileKey);
            else
                _logger.LogError("Failed to upload vehicle {Id}, status: {Status}", vehicleId, response.HttpStatusCode);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing vehicle data");
            return false;
        }
    }

    public async Task<List<string>> GetAllFileKeysAsync()
    {
        var keys = new List<string>();
        var request = new ListObjectsV2Request { BucketName = _bucketName };

        var response = await _s3Client.ListObjectsV2Async(request);
        if (response.S3Objects != null)
        {
            foreach (var obj in response.S3Objects)
            {
                if (!string.IsNullOrEmpty(obj.Key))
                    keys.Add(obj.Key);
            }
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

            using var response = await _s3Client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);
            var jsonContent = await reader.ReadToEndAsync();

            return JsonDocument.Parse(jsonContent);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("File {Key} not found in bucket {Bucket}", fileKey, _bucketName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file {Key}", fileKey);
            return null;
        }
    }
}