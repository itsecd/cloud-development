using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace Inventory.FileService.Messaging;

public class SnsSubscriberService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsSubscriberService> logger) : ISubscriberService
{
    private readonly string _topicArn =
        configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");

    private readonly string _endpoint =
        configuration["SNS:EndpointURL"]
        ?? throw new KeyNotFoundException("SNS endpoint URL was not found in configuration");

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

        logger.LogInformation("Subscription request for {topic} is successful", _topicArn);
    }
}