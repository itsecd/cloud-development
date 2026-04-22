using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace Inventory.FileService.Messaging;

/// <summary>
/// Служба подписки на SNS topic
/// </summary>
/// <param name="snsClient">SNS клиент</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логер</param>
public class SnsSubscriberService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsSubscriberService> logger) : ISubscriberService
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    private readonly string _endpoint = configuration["AWS:Resources:SNSUrl"]
        ?? throw new KeyNotFoundException("SNS callback endpoint was not found in configuration");

    ///<inheritdoc/>
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Sending subscribe request for {topic}", _topicArn);

        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = _endpoint,
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to subscribe to {topic}", _topicArn);
            throw new InvalidOperationException($"Failed to subscribe to SNS topic {_topicArn}");
        }

        logger.LogInformation("Subscription request for {topic} is successful, waiting for confirmation", _topicArn);
    }
}