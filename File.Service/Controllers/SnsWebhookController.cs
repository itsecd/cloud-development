using System.Text;
using Amazon.SimpleNotificationService.Util;
using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;

namespace File.Service.Controllers;

/// <summary>
/// Эндпоинт, на который SNS отправляет запросы подтверждения подписки и уведомления
/// </summary>
/// <param name="s3Service">Сервис записи в Minio</param>
/// <param name="logger">Логгер входящих сообщений</param>
[ApiController]
[Route("api/sns")]
public class SnsWebhookController(IS3Service s3Service, ILogger<SnsWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Обрабатывает тело SNS-запроса. Для <c>SubscriptionConfirmation</c> выполняет GET
    /// по <c>SubscribeURL</c> (с переписыванием адреса на LocalStack). Для <c>Notification</c>
    /// передаёт сообщение в <see cref="IS3Service.UploadFile"/>. Всегда отвечает 200,
    /// чтобы SNS не помечал подписчика как недоступного
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveMessage()
    {
        logger.LogInformation("SNS webhook was called");
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        try
        {
            var message = Message.ParseMessage(body);

            if (message.Type == "SubscriptionConfirmation")
            {
                logger.LogInformation("Received SubscriptionConfirmation, confirming");
                using var http = new HttpClient();
                var builder = new UriBuilder(new Uri(message.SubscribeURL))
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = 4566
                };
                var confirmation = await http.GetAsync(builder.Uri);
                if (!confirmation.IsSuccessStatusCode)
                {
                    var text = await confirmation.Content.ReadAsStringAsync();
                    logger.LogError("SubscriptionConfirmation returned {code}: {body}", confirmation.StatusCode, text);
                }
                else
                {
                    logger.LogInformation("Subscription confirmed");
                }
                return Ok();
            }

            if (message.Type == "Notification")
            {
                await s3Service.UploadFile(message.MessageText);
                logger.LogInformation("Notification was processed and uploaded to Minio");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process SNS message");
        }
        return Ok();
    }
}
