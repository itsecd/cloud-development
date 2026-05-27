using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Cloud.GeneratorFunction.Models;
using Cloud.GeneratorFunction.Services;
using System.Text.Json;

namespace Cloud.GeneratorFunction;

/// <summary>
/// Точка входа облачной функции генерации сотрудников
/// </summary>
public class Handler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly EmployeeGenerator _generator = new();

    /// <summary>
    /// Основной обработчик вызова функции
    /// </summary>
    /// <param name="request">Запрос от API Gateway</param>
    /// <returns>Ответ с JSON сотрудника или ошибкой валидации</returns>
    public FunctionResponse FunctionHandler(FunctionRequest request)
    {
        var id = TryReadId(request);
        if (id is null or <= 0)
        {
            return CreateResponse(400, """{"error":"id must be a positive integer"}""");
        }

        var employee = _generator.Generate(id.Value);
        PublishGeneratedAsync(employee).GetAwaiter().GetResult();

        return CreateResponse(200, JsonSerializer.Serialize(employee, _jsonOptions));
    }

    /// <summary>
    /// Извлекает идентификатор сотрудника из параметров запроса
    /// Поддерживает query-параметры, path-параметры и прямую строку запроса в Path/Url.
    /// </summary>
    /// <param name="request">Входной запрос функции</param>
    /// <returns>Идентификатор сотрудника или null, если извлечь не удалось</returns>
    private static int? TryReadId(FunctionRequest? request)
    {
        try
        {
            var rawId = TryGetValue(request?.QueryStringParameters, "id")
                        ?? TryGetValue(request?.PathParameters, "id")
                        ?? TryGetValue(request?.PathParams, "id");

            if (int.TryParse(rawId, out var id))
                return id;

            var path = request?.Path ?? request?.Url ?? string.Empty;
            var queryStart = path.IndexOf('?');
            if (queryStart >= 0)
            {
                var query = path[queryStart..];
                return ReadIdFromQuery(query);
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Разбирает идентификатор из строки запроса формата "?id=42"
    /// </summary>
    private static int? ReadIdFromQuery(string query)
    {
        var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 &&
                parts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(Uri.UnescapeDataString(parts[1]), out var id))
            {
                return id;
            }
        }
        return null;
    }

    /// <summary>
    /// Безопасно получает значение из словаря параметров
    /// </summary>
    private static string? TryGetValue(Dictionary<string, string>? values, string key)
    {
        if (values is null) return null;
        return values.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Публикует сгенерированного сотрудника в очередь Yandex Message Queue
    /// </summary>
    /// <param name="employee">Сотрудник для отправки</param>
    private static async Task PublishGeneratedAsync(Employee employee)
    {
        var queueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL");
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        if (string.IsNullOrWhiteSpace(queueUrl) ||
            string.IsNullOrWhiteSpace(accessKey) ||
            string.IsNullOrWhiteSpace(secretKey))
        {
            Console.WriteLine("[WARN] Message Queue settings are not configured");
            return;
        }

        var endpoint = Environment.GetEnvironmentVariable("SQS_ENDPOINT")
                       ?? "https://message-queue.api.cloud.yandex.net";
        var region = Environment.GetEnvironmentVariable("YC_REGION") ?? "ru-central1";

        using var sqs = new AmazonSQSClient(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonSQSConfig
            {
                ServiceURL = endpoint,
                AuthenticationRegion = region
            });

        var messageBody = JsonSerializer.Serialize(employee, _jsonOptions);
        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody
        });
    }

    /// <summary>
    /// Формирует стандартный HTTP-ответ функции
    /// </summary>
    /// <param name="statusCode">HTTP-статус</param>
    /// <param name="body">Тело ответа</param>
    /// <returns>Сформированный ответ</returns>
    private static FunctionResponse CreateResponse(int statusCode, string body) =>
        new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Access-Control-Allow-Origin"] = "*",
                ["Access-Control-Allow-Methods"] = "GET,OPTIONS",
                ["Access-Control-Allow-Headers"] = "Content-Type,Authorization"
            },
            Body = body
        };
}