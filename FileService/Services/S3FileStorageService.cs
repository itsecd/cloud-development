using Amazon.S3;
using Amazon.S3.Model;
using Domain.Contracts;
using System.Text.Json;

namespace FileService.Services;

public class S3FileStorageService(
    IAmazonS3 s3Client,
    IConfiguration configuration,
    ILogger<S3FileStorageService> logger) : IS3FileStorageService
{
    private readonly string _bucketName = configuration["AWS:BucketName"] ?? "vehicle-files";

    public async Task<string> SaveContractAsync(VehicleContractDto contract)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var key = $"vehicle-contracts/{contract.SystemId}/{timestamp}.json";
        var json = JsonSerializer.Serialize(contract, new JsonSerializerOptions { WriteIndented = true });

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            ContentBody = json,
            ContentType = "application/json"
        });

        logger.LogInformation("Contract {SystemId} saved to S3 bucket {Bucket} at key {Key}",
            contract.SystemId, _bucketName, key);

        return key;
    }

    public async Task EnsureBucketExistsAsync()
    {
        try
        {
            await s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = _bucketName,
                UseClientRegion = true
            });
            logger.LogInformation("S3 bucket {Bucket} created", _bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyExists" or "BucketAlreadyOwnedByYou")
        {
            logger.LogDebug("S3 bucket {Bucket} already exists", _bucketName);
        }
    }
}
