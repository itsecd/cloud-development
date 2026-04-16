using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Фоновый сервис для автоматической инициализации SNS подписки при запуске FileService.
/// Создает топик employee-events и подписывает FileService на получение уведомлений.
/// </summary>
/// <param name="snsClient">Клиент AWS SNS для взаимодействия с LocalStack.</param>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="logger">Логгер для диагностики.</param>
public class SnsInitializerService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsInitializerService> logger) : IHostedService
{
    private const string TopicName = "employee-events";

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting SNS subscription initialization");

            var fileServiceUrl = configuration["AWS:Resources:SNSUrl"]
                ?? Environment.GetEnvironmentVariable("AWS__Resources__SNSUrl")
                ?? "http://host.docker.internal:5194";

            var subscriptionEndpoint = $"{fileServiceUrl}/api/sns/notification";
            logger.LogInformation("Subscription endpoint: {Endpoint}", subscriptionEndpoint);

            var createTopicRequest = new CreateTopicRequest { Name = TopicName };
            var createTopicResponse = await snsClient.CreateTopicAsync(createTopicRequest, cancellationToken);
            logger.LogInformation("SNS topic ensured: {TopicArn}", createTopicResponse.TopicArn);

            var subscribeRequest = new SubscribeRequest
            {
                TopicArn = createTopicResponse.TopicArn,
                Protocol = "http",
                Endpoint = subscriptionEndpoint
            };

            await snsClient.SubscribeAsync(subscribeRequest, cancellationToken);
            logger.LogInformation("SNS subscription created successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize SNS subscription");
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}