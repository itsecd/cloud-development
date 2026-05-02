using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace Vehicle.EventSink.Messaging;

/// <summary>
/// Служба для подписки файлового сервиса на SNS topic при старте приложения.
/// </summary>
/// <param name="snsClient">Клиент SNS.</param>
/// <param name="configuration">Конфигурация.</param>
/// <param name="logger">Логгер.</param>
public class SnsSubscriptionService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration, ILogger<SnsSubscriptionService> logger)
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <summary>
    /// Делает попытку подписаться на SNS topic.
    /// </summary>
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Sending subscribe request for {Topic}", _topicArn);

        await EnsureTopicExists();

        var endpoint = configuration["AWS:Resources:SNSUrl"]
            ?? throw new KeyNotFoundException("SNS endpoint URL was not found in configuration");

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
            logger.LogError("Failed to subscribe to {Topic}", _topicArn);
        }
        else
        {
            logger.LogInformation(
                "Subscription request for {Topic} is successful, waiting for confirmation",
                _topicArn);
        }
    }

    /// <summary>
    /// Создает SNS topic при необходимости.
    /// Для LocalStack операция CreateTopic является безопасной и идемпотентной.
    /// </summary>
    private async Task EnsureTopicExists()
    {
        var topicName = _topicArn.Split(':').Last();

        logger.LogInformation("Ensuring SNS topic {TopicName} exists", topicName);

        await snsClient.CreateTopicAsync(
            new CreateTopicRequest
            {
                Name = topicName
            });
    }
}