using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace CompanyEmployee.FileService.Messaging;

public class SnsService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SnsService> logger)
{
    private readonly string _topicName = "companyemployee-topic";
    private string? _topicArn;

    private async Task<string> GetTopicArn()
    {
        if (_topicArn != null)
            return _topicArn;

        var response = await snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _topicName
        });

        _topicArn = response.TopicArn;
        return _topicArn;
    }

    public async Task SubscribeEndpoint()
    {
        var topicArn = await GetTopicArn();
        var endpoint = configuration["AWS:Resources:SNSUrl"];

        var request = new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "http",
            Endpoint = endpoint,
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to subscribe}");
        }
        else
        {
            logger.LogInformation("Subscription request was successful");
        }
    }
}