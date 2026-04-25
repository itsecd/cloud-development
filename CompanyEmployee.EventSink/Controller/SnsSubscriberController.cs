using System.Text;
using Amazon.SimpleNotificationService.Util;
using CompanyEmployee.EventSink.S3;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.EventSink.Controller;

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

            switch (snsMessage.Type)
            {
                case "SubscriptionConfirmation":
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
                    break;
                }
                case "Notification":
                {
                    await s3Service.UploadFile(snsMessage.MessageText);
                    logger.LogInformation("Notification was successfully processed");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while processing SNS notifications");
        }
        return Ok();
    }
}