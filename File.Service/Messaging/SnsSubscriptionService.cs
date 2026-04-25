using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace File.Service.Messaging;

/// <summary>
/// На старте приложения регистрирует HTTP-вебхук File.Service в SNS-топике
/// </summary>
/// <param name="snsClient">SNS-клиент для вызова <c>Subscribe</c></param>
/// <param name="configuration">Источник ARN топика и URL подписчика</param>
/// <param name="logger">Логгер</param>
public class SnsSubscriptionService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsSubscriptionService> logger)
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");
    private readonly string _endpoint = configuration["AWS:Resources:SNSUrl"]
        ?? throw new KeyNotFoundException("SNS subscriber endpoint was not found in configuration");

    /// <summary>
    /// Отправляет в SNS запрос <c>Subscribe</c>. Подтверждение подписки
    /// выполняется позже в <see cref="Controllers.SnsWebhookController"/>
    /// </summary>
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Subscribing endpoint {endpoint} to topic {topic}", _endpoint, _topicArn);
        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = _endpoint,
            ReturnSubscriptionArn = true
        };
        var response = await snsClient.SubscribeAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
            logger.LogError("Failed to subscribe to {topic}: {code}", _topicArn, response.HttpStatusCode);
        else
            logger.LogInformation("Subscription request for {topic} accepted; awaiting confirmation", _topicArn);
    }
}
