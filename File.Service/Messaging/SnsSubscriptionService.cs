using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Net;

namespace File.Service.Messaging;

/// <summary>
/// Служба, выполняющая HTTP-подписку File.Service на SNS-топик при старте приложения
/// </summary>
/// <param name="snsClient">Клиент Amazon SNS, разрешённый из DI</param>
/// <param name="configuration">Конфигурация приложения; содержит ARN топика и URL вебхука</param>
/// <param name="logger">Структурный логгер</param>
public class SnsSubscriptionService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsSubscriptionService> logger)
{
    /// <summary>
    /// ARN SNS-топика, на который подписывается сервис. Берётся из CloudFormation outputs (<c>AWS:Resources:SNSTopicArn</c>)
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");

    /// <summary>
    /// Публичный URL HTTP-эндпоинта, на который SNS будет присылать уведомления (контроллер <c>SnsWebhookController</c>)
    /// </summary>
    private readonly string _endpoint = configuration["AWS:Resources:SNSUrl"]
        ?? throw new KeyNotFoundException("SNS subscriber URL was not found in configuration");

    /// <summary>
    /// Регистрирует HTTP-подписку на SNS-топик. Само подтверждение подписки выполняется
    /// контроллером вебхука при получении сообщения типа <c>SubscriptionConfirmation</c>
    /// </summary>
    /// <returns>Задача, завершающаяся после получения ответа от SNS</returns>
    public async Task SubscribeEndpoint()
    {
        logger.LogInformation("Subscribing endpoint {Endpoint} to SNS topic {Topic}", _endpoint, _topicArn);
        var request = new SubscribeRequest
        {
            TopicArn = _topicArn,
            Protocol = "http",
            Endpoint = _endpoint,
            ReturnSubscriptionArn = true
        };

        var response = await snsClient.SubscribeAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
            logger.LogError("Failed to subscribe to SNS topic {Topic}: {Code}", _topicArn, response.HttpStatusCode);
        else
            logger.LogInformation("Subscription request for SNS topic {Topic} sent, awaiting confirmation", _topicArn);
    }
}
