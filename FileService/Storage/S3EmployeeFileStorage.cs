using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using File.Service.Configuration;

namespace File.Service.Storage;

/// <summary>
/// Хранилище файлов сотрудников в S3/LocalStack.
/// </summary>
public sealed class S3EmployeeFileStorage(
    IOptions<AwsStorageOptions> options,
    IAmazonS3 s3Client,
    ILogger<S3EmployeeFileStorage> logger) : IEmployeeFileStorage
{
    private readonly AwsStorageOptions _options = options.Value;

    public async Task SaveEmployeeJsonAsync(int employeeId, string json, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var key = BuildObjectKey(employeeId);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json"
        }, cancellationToken);

        logger.LogInformation("Employee file stored in bucket {Bucket} with key {Key}", _options.BucketName, key);
    }

    public async Task<string?> TryReadEmployeeJsonAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        try
        {
            var response = await s3Client.GetObjectAsync(_options.BucketName, BuildObjectKey(employeeId), cancellationToken);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, _options.BucketName);
        if (exists)
        {
            return;
        }

        await s3Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = _options.BucketName
        }, cancellationToken);
    }

    private static string BuildObjectKey(int employeeId) => $"employees/employee-{employeeId}.json";
}
