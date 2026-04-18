using Amazon.SimpleNotificationService.Util;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Service.Storage.Storage;

namespace Service.Storage.Controllers;

/// <summary>
/// Controller for getting messages from sns
/// </summary>
/// <param name="s3Service">Service to work with S3</param>
/// <param name="logger">Logger obviously</param>
[ApiController]
[Route("api/sns")]
public class SnsSubscriberController(ILogger<SnsSubscriberController> logger, IS3Service s3service) : ControllerBase
{
    /// <summary>
    /// Webhook that gets notifications from sns topic
    /// </summary>
    /// <remarks> 
    /// As well confirms subscription at the messaging beginning. Returns cod 200 anyway
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
                await s3service.UploadFile(snsMessage.MessageText);
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
