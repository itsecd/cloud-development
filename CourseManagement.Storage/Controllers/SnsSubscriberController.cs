using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Amazon.SimpleNotificationService.Util;
using CourseManagement.Storage.Services;

namespace CourseManagement.Storage.Controllers;

/// <summary>
/// Контроллер для приема сообщений от SNS
/// </summary>
/// <param name="s3Service">Сервис для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/sns")]
public class SnsSubscriberController(IS3Service s3Service, ILogger<SnsSubscriberController> logger) : ControllerBase
{
    /// <summary>
    /// Вебхук, который получает оповещения из SNS топика/подтверждает подписку на топик
    /// </summary>
    [HttpPost]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ReceiveMessage()
    {
        logger.LogInformation("SNS webhook was called");

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();

            var json = JsonNode.Parse(jsonContent);
            if (json == null)
            {
                logger.LogWarning("Received invalid JSON from SNS");
                return Ok();
            }

            var messageType = json["Type"]?.GetValue<string>() ?? "";
            logger.LogInformation("SNS message type: {Type}", messageType);

            if (messageType == "SubscriptionConfirmation")
            {
                var subscribeUrl = json["SubscribeURL"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(subscribeUrl))
                {
                    logger.LogInformation("Confirming subscription...");

                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(subscribeUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        logger.LogInformation("Subscription confirmed successfully");
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        logger.LogWarning("Subscription confirmation returned {Status}: {Body}",
                            response.StatusCode, body);
                    }
                }
                return Ok();
            }

            if (messageType == "Notification")
            {
                var messageText = json["Message"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(messageText))
                {
                    await s3Service.UploadFile(messageText);
                    logger.LogInformation("Notification processed and file uploaded to S3");
                }
                else
                {
                    logger.LogWarning("Notification received but Message field is empty");
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while processing SNS notifications");
            return Ok();
        }
    }
}
