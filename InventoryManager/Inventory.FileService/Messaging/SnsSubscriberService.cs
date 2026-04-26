using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace Inventory.FileService.Messaging;

/// <summary>
/// Сервис для подписки HTTP-endpoint на SNS-топик
/// </summary>
/// <param name="snsClient"> Клиент Amazon SNS для отправки запроса на подписку</param>
/// <param name="configuration"> Конфигурация приложения, содержащая ARN SNS-топика и URL endpoint</param>
/// <param name="logger"> Сервис логирования процесса подписки</param>
public class SnsSubscriberService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration,
                                  ILogger<SnsSubscriberService> logger) : ISubscriberService
{
    /// <summary>
    /// ARN SNS-топика, на который должен быть подписан endpoint
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");

    /// <summary>
    /// URL HTTP-endpoint, который будет получать уведомления от SNS
    /// </summary>
    private readonly string _endpoint = configuration["SNS:EndpointURL"]
        ?? throw new KeyNotFoundException("SNS endpoint URL was not found in configuration");

    /// <summary>
    /// Отправляет запрос на подписку HTTP-endpoint на указанный SNS-топик
    /// </summary>
    /// <returns> Асинхронная операция подписки endpoint на SNS-топик</returns>
    /// <exception cref="InvalidOperationException">
    /// Возникает, если запрос на подписку завершился с неуспешным HTTP-статусом
    /// </exception>
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