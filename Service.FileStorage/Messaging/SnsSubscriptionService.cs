using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace Service.FileStorage.Messaging;

/// <summary>
/// Служба для подписки на SNS топик при старте приложения
/// </summary>
/// <param name="snsClient">Клиент SNS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SnsSubscriptionService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsSubscriptionService> logger)
{
    /// <summary>
    /// Уникальный идентификатор топика SNS
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");

    /// <summary>
    /// Делает попытку подписаться на топик SNS
    /// </summary>
    public async Task SubscribeEndpoint()
    {
        var endpoint = configuration["AWS:Resources:SNSUrl"];
        logger.LogInformation("Subscribing to {Topic} with endpoint {Endpoint}", _topicArn, endpoint);

        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = endpoint,
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
            logger.LogError("Failed to subscribe to {Topic}", _topicArn);
        else
            logger.LogInformation("Subscription request for {Topic} sent, waiting for confirmation", _topicArn);
    }
}
