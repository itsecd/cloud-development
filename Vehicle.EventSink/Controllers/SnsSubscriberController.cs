using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;
using Vehicle.EventSink.Storage;

namespace Vehicle.EventSink.Controllers;

/// <summary>
/// Контроллер для приема сообщений от SNS.
/// </summary>
/// <param name="s3Service">Служба для работы с S3.</param>
/// <param name="logger">Логгер.</param>
[ApiController]
[Route("api/sns")]
public class SnsSubscriberController(IS3Service s3Service, ILogger<SnsSubscriberController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ReceiveMessage()
    {
        logger.LogInformation("SNS webhook was called");

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();

            var rootNode = JsonNode.Parse(jsonContent) ?? throw new ArgumentException("SNS message is not valid JSON");

            var messageType = rootNode["Type"]?.GetValue<string>();

            if (messageType == "SubscriptionConfirmation")
            {
                logger.LogInformation("SubscriptionConfirmation was received");

                var subscribeUrl = rootNode["SubscribeURL"]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(subscribeUrl))
                {
                    throw new ArgumentException("SubscribeURL was not found in SNS message");
                }

                using var httpClient = new HttpClient();

                var builder = new UriBuilder(new Uri(subscribeUrl))
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

            if (messageType == "Notification")
            {
                var messageText = rootNode["Message"]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(messageText))
                {
                    throw new ArgumentException("Message field was not found in SNS notification");
                }

                var uploaded = await s3Service.UploadFile(messageText);

                if (uploaded)
                {
                    logger.LogInformation("Notification was successfully processed");
                }
                else
                {
                    logger.LogWarning("Notification was received, but file was not uploaded");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while processing SNS notification");
        }

        return Ok();
    }
}