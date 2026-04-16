using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace ResidentialBuilding.FileService.Service.Messaging;

/// <summary>
/// Служба для подписки на SNS на старте приложения
/// </summary>
/// <param name="snsClient">Клиент SNS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SnsSubscriptionService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsSubscriptionService> logger) : ISubscriptionService
{
    /// <summary>
    /// Уникальный идентификатор топика SNS
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"] ??
                                        throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <summary>
    /// Делает попытку подписаться на топик SNS
    /// </summary>
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Sending subscribe request for topic {topic}", _topicArn);
        var endpoint = configuration["AWS:Resources:SNSUrl"];

        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = endpoint,
            ReturnSubscriptionArn = true
        };
        var response = await snsClient.SubscribeAsync(request);
        
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to subscribe to topic {topic}", _topicArn);
        }
        else
        {
            logger.LogInformation("Subscription request for topic {topic} is successfully, waiting for confirmation", 
                _topicArn);
        }
    }
}