using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Service.Storage.Broker;

public class SnsSubscriptionService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SnsSubscriptionService> logger)
{
    /// <summary>
    /// Unique topic sns identifier
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"] ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <summary>
    /// tries to subscribe on sns topic
    /// </summary>
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Sending subscride request for {topic}", _topicArn);
        var endpoint = configuration["AWS:Resources:SNSUrl"];
        logger.LogInformation($"SNS Endpoint: {endpoint}");

        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = endpoint,
            ReturnSubscriptionArn = true
        };
        var response = await snsClient.SubscribeAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
            logger.LogError("Failed to subscribe to {topic}", _topicArn);
        else
            logger.LogInformation("Subscripltion request for {topic} is seccessfull, waiting for confirmation", _topicArn);
    }
}
