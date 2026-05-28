using System.Text.Json;
using File.Service.Configuration;
using File.Service.Storage;
using Microsoft.Extensions.Options;

namespace File.Service.Sns;

/// <summary>
/// Реализация обработчика SNS-уведомлений: подтверждает подписки и
/// сохраняет поступающие сообщения о сотрудниках в объектное хранилище.
/// </summary>
public sealed class SnsNotificationHandler(
    IEmployeeFileStorage storage,
    IHttpClientFactory httpClientFactory,
    IOptions<AwsStorageOptions> options,
    ILogger<SnsNotificationHandler> logger) : ISnsNotificationHandler
{
    /// <summary>Имя именованного <see cref="HttpClient"/>, используемого для подтверждения подписки.</summary>
    public const string ConfirmationHttpClientName = "sns-confirmation";

    private const string MessageTypeHeader = "x-amz-sns-message-type";
    private const string SubscriptionConfirmationType = "SubscriptionConfirmation";
    private const string NotificationType = "Notification";

    private readonly AwsStorageOptions _options = options.Value;

    public async Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
        {
            logger.LogWarning("Получено пустое SNS-уведомление");
            return Results.BadRequest();
        }

        var messageType = context.Request.Headers[MessageTypeHeader].ToString();

        if (string.Equals(messageType, SubscriptionConfirmationType, StringComparison.OrdinalIgnoreCase))
        {
            await ConfirmSubscriptionAsync(body, cancellationToken);
            return Results.Ok();
        }

        var payloadJson = ExtractPayload(body, messageType);

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            logger.LogWarning("Получено SNS-уведомление без полезной нагрузки. Type={Type}", messageType);
            return Results.Ok();
        }

        await StoreEmployeePayloadAsync(payloadJson, cancellationToken);
        return Results.Ok();
    }

    private async Task StoreEmployeePayloadAsync(string payloadJson, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);

            if (!document.RootElement.TryGetProperty("id", out var idProperty) ||
                idProperty.ValueKind != JsonValueKind.Number)
            {
                logger.LogWarning("SNS-сообщение не содержит идентификатор сотрудника: {Body}", payloadJson);
                return;
            }

            var employeeId = idProperty.GetInt32();
            await storage.SaveEmployeeJsonAsync(employeeId, payloadJson, cancellationToken);

            logger.LogInformation("Сотрудник {EmployeeId} экспортирован в объектное хранилище.", employeeId);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Не удалось разобрать SNS-сообщение: {Body}", payloadJson);
        }
    }

    private static string? ExtractPayload(string body, string? messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType) ||
            string.Equals(messageType, NotificationType, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("Message", out var messageProperty) &&
                    messageProperty.ValueKind == JsonValueKind.String)
                {
                    return messageProperty.GetString();
                }
            }
            catch (JsonException)
            {
                // body не валидный JSON-конверт SNS — считаем, что это raw payload.
            }
        }

        return body;
    }

    private async Task ConfirmSubscriptionAsync(string body, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("SubscribeURL", out var urlProperty) ||
                urlProperty.ValueKind != JsonValueKind.String)
            {
                logger.LogWarning("SubscriptionConfirmation не содержит SubscribeURL: {Body}", body);
                return;
            }

            var subscribeUrl = urlProperty.GetString();
            if (string.IsNullOrWhiteSpace(subscribeUrl))
            {
                return;
            }

            var normalizedUrl = NormalizeSubscribeUrl(subscribeUrl);

            var client = httpClientFactory.CreateClient(ConfirmationHttpClientName);
            client.Timeout = TimeSpan.FromSeconds(5);

            using var response = await client.GetAsync(normalizedUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("SNS-подписка подтверждена. SubscribeURL={SubscribeUrl}", normalizedUrl);
            }
            else
            {
                logger.LogWarning(
                    "Не удалось подтвердить SNS-подписку (HTTP {Status}). LocalStack обычно подтверждает HTTP-подписки автоматически, поэтому продолжаем работу.",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось подтвердить SNS-подписку. LocalStack обычно подтверждает HTTP-подписки автоматически, продолжаем работу.");
        }
    }

    /// <summary>
    /// LocalStack возвращает SubscribeURL с собственного внутреннего хоста и порта
    /// (например, <c>http://localhost.localstack.cloud:4566/...</c>), которые недоступны
    /// с хост-машины, особенно когда Aspire проксирует edge-порт через случайный хостовый порт.
    /// Поэтому хост и порт принудительно заменяем на те, по которым LocalStack реально доступен из нашего процесса.
    /// </summary>
    private Uri NormalizeSubscribeUrl(string subscribeUrl)
    {
        var original = new Uri(subscribeUrl);
        var actual = new Uri(_options.ServiceUrl);

        var builder = new UriBuilder(original)
        {
            Scheme = actual.Scheme,
            Host = actual.Host,
            Port = actual.Port
        };

        return builder.Uri;
    }
}
