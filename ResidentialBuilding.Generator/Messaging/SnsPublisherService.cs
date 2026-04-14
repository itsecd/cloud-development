using System.Net;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Generator.DTO;

namespace Generator.Messaging;

/// <summary>
/// Служба для отправки сообщений в SNS
/// </summary>
/// <param name="client">Клиент SNS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SnsPublisherService(IAmazonSimpleNotificationService client, IConfiguration configuration, ILogger<SnsPublisherService> logger) : IProducerService
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
            var response = await client.PublishAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Residential building {id} was sent to sink via SNS", residentialBuilding.Id);
            else
                throw new Exception($"SNS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send residential building through SNS topic");
        }

    }
}