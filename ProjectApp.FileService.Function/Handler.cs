using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace ProjectApp.FileService.Function;

public class Handler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public object FunctionHandler(QueueRequest request)
    {
        var processed = ProcessMessagesAsync(request).GetAwaiter().GetResult();
        return new
        {
            statusCode = 200,
            body = JsonSerializer.Serialize(new { processed })
        };
    }

    private static async Task<int> ProcessMessagesAsync(QueueRequest? request)
    {
        using var s3 = CreateS3Client();
        var bucketName = Environment.GetEnvironmentVariable("S3_BUCKET") ?? "credit-applications";
        var processed = 0;

        foreach (var queueEvent in request?.Messages ?? [])
        {
            var rawBody = queueEvent.Details?.Message?.Body;
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                continue;
            }

            var generatedEvent = TryDeserializeEvent(rawBody);
            if (generatedEvent is null)
            {
                Console.WriteLine("[WARN] Failed to deserialize queue message");
                continue;
            }

            var key = $"credit-applications/{generatedEvent.Id}-{generatedEvent.OccurredAtUtc:yyyyMMddHHmmssfff}.json";
            var payload = JsonSerializer.Serialize(generatedEvent.Application, JsonOptions);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = "application/json"
            });

            processed++;
            Console.WriteLine($"[INFO] Saved credit application {generatedEvent.Id} to {bucketName}/{key}");
        }

        return processed;
    }

    private static CreditApplicationGeneratedEvent? TryDeserializeEvent(string rawBody)
    {
        foreach (var candidate in GetBodyCandidates(rawBody))
        {
            try
            {
                var evt = JsonSerializer.Deserialize<CreditApplicationGeneratedEvent>(candidate, JsonOptions);
                if (evt?.Application is not null)
                {
                    return evt;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetBodyCandidates(string rawBody)
    {
        var candidates = new List<string> { rawBody };

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(rawBody));
            candidates.Add(decoded);
        }
        catch
        {
        }

        return candidates;
    }

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

public class QueueRequest
{
    [JsonPropertyName("messages")]
    public List<QueueEvent> Messages { get; set; } = new();
}

public class QueueEvent
{
    [JsonPropertyName("details")]
    public QueueEventDetails? Details { get; set; }
}

public class QueueEventDetails
{
    [JsonPropertyName("message")]
    public QueueMessage? Message { get; set; }
}

public class QueueMessage
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class CreditApplicationGeneratedEvent
{
    public int Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public CreditApplication Application { get; set; } = new();
}

public class CreditApplication
{
    public int Id { get; set; }
    public string CreditType { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public double InterestRate { get; set; }
    public DateOnly ApplicationDate { get; set; }
    public bool RequiresInsurance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DecisionDate { get; set; }
    public decimal? ApprovedAmount { get; set; }
}
