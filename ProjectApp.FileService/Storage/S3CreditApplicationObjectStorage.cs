using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using ProjectApp.Domain.Messaging;
using ProjectApp.FileService.Options;

namespace ProjectApp.FileService.Storage;

public class S3CreditApplicationObjectStorage(
    IAmazonS3 s3Client,
    IOptions<FilePersistenceOptions> options,
    ILogger<S3CreditApplicationObjectStorage> logger) : ICreditApplicationObjectStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private bool _bucketReady;

    public async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady)
        {
            return;
        }

        var buckets = await s3Client.ListBucketsAsync(cancellationToken);
        if (buckets.Buckets?.All(b => b.BucketName != options.Value.BucketName) != false)
        {
            await s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = options.Value.BucketName
            }, cancellationToken);
        }

        _bucketReady = true;
    }

    public async Task SaveAsync(CreditApplicationGeneratedEvent evt, CancellationToken cancellationToken)
    {
        var key = ICreditApplicationObjectStorage.BuildObjectKey(evt.Id);
        var payload = JsonSerializer.Serialize(evt.Application, JsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json"
        }, cancellationToken);

        logger.LogInformation("Saved credit application {Id} to object storage as {Key}", evt.Id, key);
    }
}
