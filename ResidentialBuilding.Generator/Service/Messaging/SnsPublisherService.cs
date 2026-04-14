using System.Net;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Generator.DTO;

namespace Generator.Service.Messaging;

/// <summary>
/// Сервис для отправки сообщений в SNS
/// </summary>
/// <param name="client">Клиент SNS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SnsPublisherService(IAmazonSimpleNotificationService client, IConfiguration configuration, ILogger<SnsPublisherService> logger) : IPublisherService
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
                                        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    ///<inheritdoc/>
    public async Task SendMessage(ResidentialBuildingDto residentialBuilding)
    {
        try
        {
            var json = JsonSerializer.Serialize(residentialBuilding);
            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };
            
            var statusCode = (await client.PublishAsync(request)).HttpStatusCode;
            if (statusCode != HttpStatusCode.OK)
            {
                throw new Exception($"SNS returned status code {statusCode}");
            }
            
            logger.LogInformation("Residential building with id={Id} was sent to file service via SNS", residentialBuilding.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send residential building through SNS topic");
        }

    }
}