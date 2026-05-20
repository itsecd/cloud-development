using System.Text.Json;
using File.Service.Background;
using File.Service.Configuration;
using File.Service.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services.Configure<AwsStorageOptions>(builder.Configuration.GetSection(AwsStorageOptions.SectionName));

builder.Services.AddSingleton<FileExportInfrastructureState>();
builder.Services.AddSingleton<IEmployeeFileStorage, S3EmployeeFileStorage>();
builder.Services.AddHttpClient("sns-confirmation");
builder.Services.AddHostedService<SnsFileExportWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "File.Service",
    description = "Файловый сервис, сохраняющий сведения о сотрудниках в объектное хранилище",
    endpoints = new[] { "/ready", "/files/{id}", "POST /sns/notifications" }
}));

app.MapGet("/ready", (FileExportInfrastructureState state) =>
{
    return state.IsInitialized
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/files/{id:int}", async (int id, IEmployeeFileStorage storage, CancellationToken cancellationToken) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { message = "Идентификатор сотрудника должен быть больше нуля." });
    }

    var content = await storage.TryReadEmployeeJsonAsync(id, cancellationToken);
    if (string.IsNullOrWhiteSpace(content))
    {
        return Results.NotFound(new { message = $"Файл для сотрудника {id} не найден." });
    }

    return Results.Text(content, "application/json");
});

app.MapPost("/sns/notifications", async (
    HttpContext context,
    IEmployeeFileStorage storage,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("SnsNotifications");

    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(cancellationToken);

    if (string.IsNullOrWhiteSpace(body))
    {
        logger.LogWarning("Получено пустое SNS-уведомление");
        return Results.BadRequest();
    }

    var messageType = context.Request.Headers["x-amz-sns-message-type"].ToString();

    if (string.Equals(messageType, "SubscriptionConfirmation", StringComparison.OrdinalIgnoreCase))
    {
        await ConfirmSubscriptionAsync(body, httpClientFactory, logger, cancellationToken);
        return Results.Ok();
    }

    var payloadJson = ExtractPayload(body, messageType);

    if (string.IsNullOrWhiteSpace(payloadJson))
    {
        logger.LogWarning("Получено SNS-уведомление без полезной нагрузки. Type={Type}", messageType);
        return Results.Ok();
    }

    try
    {
        using var document = JsonDocument.Parse(payloadJson);

        if (!document.RootElement.TryGetProperty("id", out var idProperty) ||
            idProperty.ValueKind != JsonValueKind.Number)
        {
            logger.LogWarning("SNS-сообщение не содержит идентификатор сотрудника: {Body}", payloadJson);
            return Results.Ok();
        }

        var employeeId = idProperty.GetInt32();
        await storage.SaveEmployeeJsonAsync(employeeId, payloadJson, cancellationToken);

        logger.LogInformation("Сотрудник {EmployeeId} экспортирован в объектное хранилище.", employeeId);
    }
    catch (JsonException ex)
    {
        logger.LogWarning(ex, "Не удалось разобрать SNS-сообщение: {Body}", payloadJson);
    }

    return Results.Ok();
});

app.Run();

static string? ExtractPayload(string body, string? messageType)
{
    if (string.IsNullOrWhiteSpace(messageType) ||
        string.Equals(messageType, "Notification", StringComparison.OrdinalIgnoreCase))
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

static async Task ConfirmSubscriptionAsync(
    string body,
    IHttpClientFactory httpClientFactory,
    ILogger logger,
    CancellationToken cancellationToken)
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

        // SubscribeURL может содержать DNS вида localhost.localstack.cloud,
        // который не резолвится с хоста — заменяем на обычный localhost.
        var normalizedUrl = subscribeUrl.Replace(
            "localhost.localstack.cloud",
            "localhost",
            StringComparison.OrdinalIgnoreCase);

        var client = httpClientFactory.CreateClient("sns-confirmation");
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

public partial class Program;
