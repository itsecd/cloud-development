using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using ProjectApp.Domain.Events;

namespace ProjectApp.FileService.Storage;

public class MinioCreditApplicationObjectStorage(
    IAmazonS3 s3,
    IConfiguration configuration) : ICreditApplicationObjectStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        var bucketName = GetBucketName();
        try
        {
            await s3.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
        }
    }

    public async Task SaveAsync(CreditApplicationGeneratedEvent generatedEvent, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(generatedEvent.Application, JsonOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = GetBucketName(),
            Key = ICreditApplicationObjectStorage.BuildObjectKey(generatedEvent.Id),
            InputStream = stream,
            ContentType = "application/json"
        }, cancellationToken);
    }

    private string GetBucketName()
        => configuration["Minio:BucketName"] ?? "credit-applications";
}
