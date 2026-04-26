using Amazon.SimpleNotificationService.Util;
using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace File.Service.Controllers;

/// <summary>
/// Контроллер для приема сообщений от SNS
/// </summary>
/// <param name="s3Service">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/sns")]
public class SnsSubscriberController(IS3Service s3Service, ILogger<SnsSubscriberController> logger) : ControllerBase
{
    /// <summary>
    /// Вебхук, который получает оповещения из SNS топика
    /// </summary>
    /// <remarks>
    /// Используется не только, чтобы получать оповещения,
    /// но и для того, чтобы подтвердить подписку при
    /// инициализации информационного обмена.
    /// В любом случае должен возвращать 200
    /// </remarks>
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
