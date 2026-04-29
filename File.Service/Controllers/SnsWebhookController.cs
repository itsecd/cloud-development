using Amazon.SimpleNotificationService.Util;
using File.Service.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace File.Service.Controllers;

/// <summary>
/// Контроллер вебхука, принимающий уведомления от SNS-топика
/// </summary>
/// <param name="storage">Служба объектного хранилища, в которое сохраняется тело уведомления</param>
/// <param name="logger">Структурный логгер</param>
[ApiController]
[Route("api/sns")]
public class SnsWebhookController(IObjectStorage storage, ILogger<SnsWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Принимает HTTP-запросы от SNS. Подтверждает подписку, если получено сообщение типа
    /// <c>SubscriptionConfirmation</c>, иначе сохраняет полезную нагрузку в объектное хранилище
    /// </summary>
    /// <returns><c>200 OK</c> в любом случае — иначе SNS будет повторять отправку</returns>
    [HttpPost]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Receive()
    {
        logger.LogInformation("SNS webhook invoked");
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();
            var snsMessage = Message.ParseMessage(jsonContent);

            if (snsMessage.Type == "SubscriptionConfirmation")
            {
                logger.LogInformation("SubscriptionConfirmation received");
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

                logger.LogInformation("SNS subscription confirmed");
                return Ok();
            }

            if (snsMessage.Type == "Notification")
            {
                await storage.UploadProject(snsMessage.MessageText);
                logger.LogInformation("SNS notification successfully processed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while processing SNS notification");
        }

        return Ok();
    }
}
