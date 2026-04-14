using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace CourseManagement.Storage.Messaging;

/// <summary>
/// Сервис для подписки на SNS на старте приложения
/// </summary>
/// <param name="logger">Логгер</param>
/// <param name="snsClient">Клиент SNS</param>
/// <param name="configuration">Конфигурация</param>
public class SnsSubscriberService(ILogger<SnsSubscriberService> logger, IAmazonSimpleNotificationService snsClient, IConfiguration configuration) : ISubscriberService
{
    /// <summary>
    /// Уникальный идентификатор топика SNS
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"] ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <inheritdoc/>
    public async Task<bool> SubscribeEndpoint()
    {
        logger.LogInformation("Sending subscride request for {topic}", _topicArn);

        var endpoint = configuration["AWS:Resources:SNSEndpointUrl"];

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

        return response.HttpStatusCode == HttpStatusCode.OK;
    }
}