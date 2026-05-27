using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Cloud.FileServiceFunction.Models;
using System.Text;
using System.Text.Json;

namespace Cloud.FileServiceFunction.Services;

/// <summary>
/// Сервис сохранения данных сотрудников в Yandex Object Storage (S3-совместимый)
/// </summary>
public class S3StorageService
{
    private readonly IAmazonS3 _s3Client = CreateS3Client();
    private readonly string _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET") ?? "cloud-employee-files";
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// Обрабатывает пакет сообщений из очереди и сохраняет сотрудников в Object Storage
    /// </summary>
    /// <param name="request">Сообщение из очереди</param>
    /// <returns>Количество успешно обработанных сообщений</returns>
    public async Task<int> ProcessMessagesAsync(QueueRequest? request)
    {
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

            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = "application/json"
            });

            processed++;
            Console.WriteLine($"[INFO] Saved employee {employee.Id} to {_bucketName}/{key}");
        }

        return processed;
    }

    /// <summary>
    /// Создаёт и настраивает клиент для взаимодействия с Yandex Object Storage
    /// Использует переменные окружения: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, S3_ENDPOINT, YC_REGION
    /// </summary>
    /// <returns>Настроенный экземпляр</returns>
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
