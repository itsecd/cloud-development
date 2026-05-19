using Amazon.SimpleNotificationService.Util;
using CompanyEmployee.DtoModel;
using CompanyEmployee.FileService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CompanyEmployee.FileService.Controllers;

[ApiController]
[Route("api/sns")]
public class SnsController(MinioService minioService, ILogger<SnsController> logger): ControllerBase
{
    [HttpPost]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ReceiveMessage()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();
            var snsMessage = Message.ParseMessage(jsonContent);

            if (snsMessage.Type == "SubscriptionConfirmation")
            {
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
                }

                logger.LogInformation("Subscription confirmed");

                return Ok();
            }

            if (snsMessage.Type == "Notification")
            {
                var employee = JsonSerializer.Deserialize<ModelDTO>(snsMessage.MessageText);

                if (employee is null)
                    return Ok();

                var fileName = $"employee-{employee.Id}.json";

                await minioService.UploadJsonAsync(fileName, JsonSerializer.Serialize(employee));

                logger.LogInformation("Employee was saved to Minio");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,"Exception occurred while processing SNS notification");
        }

        return Ok();
    }
}