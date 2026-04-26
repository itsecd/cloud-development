using Amazon.SimpleNotificationService.Util;
using Inventory.FileService.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Inventory.FileService.Controllers;

/// <summary>
/// Контроллер для получения и обработки сообщений из SNS
/// </summary>
/// <param name="s3Service"> Сервис для работы с S3-хранилищем</param>
/// <param name="logger"> Сервис логирования работы контроллера</param>
[ApiController]
[Route("api/sns")]
public class SnsSubscriberController(IS3Service s3Service, ILogger<SnsSubscriberController> logger) : ControllerBase
{
    /// <summary>
    /// Принимает входящее сообщение от SNS, подтверждает подписку или обрабатывает уведомление
    /// </summary>
    /// <returns> Результат обработки входящего SNS-сообщения</returns>
    [HttpPost]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ReceiveMessage()
    {
        logger.LogInformation("SNS webhook was called");

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();
            var snsMessage = Message.ParseMessage(jsonContent);

            if (snsMessage.Type == "SubscriptionConfirmation")
            {
                logger.LogInformation("SubscriptionConfirmation was received");

                using var httpClient = new HttpClient();
                var builder = new UriBuilder(new Uri(snsMessage.SubscribeURL))
                {
                    Scheme = "http",
                    Host = "localhost",
                    Port = 4566
                };

                var response = await httpClient.GetAsync(builder.Uri);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    throw new Exception($"SubscriptionConfirmation returned {response.StatusCode}: {body}");
                }

                logger.LogInformation("Subscription was successfully confirmed");
                return Ok();
            }

            if (snsMessage.Type == "Notification")
            {
                await s3Service.UploadFile(snsMessage.MessageText);
                logger.LogInformation("Notification was successfully processed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while processing SNS notifications");
        }

        return Ok();
    }
}