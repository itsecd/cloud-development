using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Cloud.FileServiceFunction;

public class Handler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
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
        var bucketName = Environment.GetEnvironmentVariable("S3_BUCKET") ?? "cloud-employee-files";
        var processed = 0;

        foreach (var queueEvent in request?.Messages ?? [])
        {
            var rawBody = queueEvent.Details?.Message?.Body;
            if (string.IsNullOrWhiteSpace(rawBody))
                continue;

            Employee? employee = null;
            try
            {
                employee = JsonSerializer.Deserialize<Employee>(rawBody, _jsonOptions);
            }
            catch
            {
                Console.WriteLine("[WARN] Failed to deserialize queue message");
            }

            if (employee is null)
                continue;

            var key = $"cloud_employee_{employee.Id}.json";
            var payload = JsonSerializer.Serialize(employee, _jsonOptions);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = "application/json"
            });

            processed++;
            Console.WriteLine($"[INFO] Saved employee {employee.Id} to {bucketName}/{key}");
        }

        return processed;
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
    [JsonPropertyName("messages")] public List<QueueEvent> Messages { get; set; } = new();
}

public class QueueEvent
{
    [JsonPropertyName("details")] public QueueEventDetails? Details { get; set; }
}

public class QueueEventDetails
{
    [JsonPropertyName("message")] public QueueMessage? Message { get; set; }
}

public class QueueMessage
{
    [JsonPropertyName("message_id")] public string MessageId { get; set; } = string.Empty;
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
}

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public decimal Salary { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsFired { get; set; }
    public DateOnly? FiredDate { get; set; }
}