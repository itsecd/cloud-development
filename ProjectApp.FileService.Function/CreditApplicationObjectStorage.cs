using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace ProjectApp.FileService.Function;

public class CreditApplicationObjectStorage
{
    public async Task SaveAsync(CreditApplicationGeneratedEvent generatedEvent)
    {
        using var s3 = CreateS3Client();
        var bucketName = Environment.GetEnvironmentVariable("S3_BUCKET") ?? "credit-applications";
        var key = BuildObjectKey(generatedEvent.Id);
        var payload = JsonSerializerDefaultsProvider.SerializeIndented(generatedEvent.Application);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json"
        });

        Console.WriteLine($"[INFO] Saved credit application {generatedEvent.Id} to {bucketName}/{key}");
    }

    private static string BuildObjectKey(int id)
        => $"credit-applications/{id}.json";

    private static IAmazonS3 CreateS3Client()
    {
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? string.Empty;
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? string.Empty;
        var endpoint = Environment.GetEnvironmentVariable("S3_ENDPOINT")
            ?? "https://storage.yandexcloud.net";

        return new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                AuthenticationRegion = Environment.GetEnvironmentVariable("YC_REGION") ?? "ru-central1",
                ForcePathStyle = true
            });
    }
}
