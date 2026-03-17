using CreditApp.Domain.Entities;
using CreditApp.FileService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CreditApp.FileService.Controllers;

/// <summary>
/// Контроллер для приёма уведомлений от AWS SNS о новых кредитных заявках
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class NotificationController(MinioStorageService minioStorage, IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions, ILogger<NotificationController> logger) : ControllerBase
{
    /// <summary>
    /// Webhook для приёма SNS уведомлений и сохранения кредитных заявок в MinIO
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат обработки уведомления</returns>
    /// <remarks>
    /// Обрабатывает два типа SNS сообщений:
    /// - SubscriptionConfirmation: подтверждение подписки на топик
    /// - Notification: новая кредитная заявка для сохранения
    /// </remarks>
    /// <response code="200">Уведомление успешно обработано</response>
    /// <response code="400">Некорректный формат данных</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReceiveSnsNotification(CancellationToken cancellationToken)
    {
        try
        {
            // Читаем тело запроса от SNS
            using var reader = new StreamReader(Request.Body);
            var bodyContent = await reader.ReadToEndAsync(cancellationToken);

            logger.LogInformation("Получено SNS уведомление: {Body}", bodyContent);

            var body = JsonSerializer.Deserialize<JsonElement>(bodyContent);

            if (body.TryGetProperty("Type", out var typeElement))
            {
                var messageType = typeElement.GetString();

                // Обработка подтверждения подписки (происходит один раз при первом запуске)
                if (messageType == "SubscriptionConfirmation")
                {
                    logger.LogInformation("Получено подтверждение подписки SNS");

                    // SNS требует перейти по специальному URL для подтверждения подписки
                    if (body.TryGetProperty("SubscribeURL", out var subscribeUrlElement))
                    {
                        var subscribeUrl = subscribeUrlElement.GetString();

                        if (!string.IsNullOrEmpty(subscribeUrl))
                        {
                            logger.LogInformation("Подтверждение подписки через URL: {Url}", subscribeUrl);

                            // Выполняем HTTP GET запрос для подтверждения подписки
                            using var httpClient = httpClientFactory.CreateClient();
                            var response = await httpClient.GetAsync(subscribeUrl, cancellationToken);

                            if (response.IsSuccessStatusCode)
                            {
                                logger.LogInformation("Подписка SNS успешно подтверждена");
                            }
                            else
                            {
                                logger.LogWarning("Не удалось подтвердить подписку SNS: {StatusCode}", response.StatusCode);
                            }
                        }
                    }

                    return Ok(new { message = "Subscription confirmed" });
                }

                // Обработка уведомления о новой кредитной заявке
                if (messageType == "Notification")
                {
                    if (body.TryGetProperty("Message", out var messageElement))
                    {
                        var messageJson = messageElement.GetString();

                        if (string.IsNullOrEmpty(messageJson))
                        {
                            logger.LogWarning("Получено пустое сообщение от SNS");
                            return BadRequest("Empty message");
                        }

                        // Десериализуем кредитную заявку из сообщения
                        var creditApplication = JsonSerializer.Deserialize<CreditApplication>(messageJson);

                        if (creditApplication == null)
                        {
                            logger.LogWarning("Не удалось десериализовать CreditApplication");
                            return BadRequest("Invalid credit application data");
                        }

                        logger.LogInformation(
                            "Получена кредитная заявка {Id} через SNS",
                            creditApplication.Id);

                        // Убеждаемся, что bucket существует в MinIO
                        // Используем CancellationToken.None чтобы операция завершилась даже если HTTP запрос отменен
                        await minioStorage.EnsureBucketExistsAsync(CancellationToken.None);

                        // Формируем уникальное имя файла с временной меткой
                        var fileName = $"credit-application-{creditApplication.Id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                        var jsonContent = JsonSerializer.Serialize(creditApplication, jsonOptions);

                        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

                        // Сохраняем заявку в MinIO для долговременного хранения
                        // Используем CancellationToken.None чтобы файл точно был сохранен
                        var uploadedPath = await minioStorage.UploadFileAsync(
                            fileName,
                            stream,
                            "application/json",
                            CancellationToken.None);

                        logger.LogInformation(
                            "Кредитная заявка {Id} сохранена в MinIO: {Path}",
                            creditApplication.Id,
                            uploadedPath);

                        return Ok(new { message = "Credit application saved", path = uploadedPath });
                    }
                }
            }

            logger.LogWarning("Получено неизвестное SNS сообщение");
            return BadRequest("Unknown message type");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке SNS уведомления");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
