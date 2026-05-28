using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using File.Service.Configuration;
using Microsoft.Extensions.Options;

namespace File.Service.Background;

/// <summary>
/// Фоновый обработчик, обеспечивающий подписку файлового сервиса
/// на SNS-топик через HTTP-эндпойнт. Сами сообщения принимаются по HTTP
/// в <c>Program.cs</c> и обрабатываются <see cref="IEmployeeFileStorage"/>.
/// </summary>
public sealed class SnsFileExportWorker(
    IOptions<AwsStorageOptions> options,
    IAmazonSimpleNotificationService snsClient,
    ILogger<SnsFileExportWorker> logger,
    FileExportInfrastructureState state) : BackgroundService
{
    private const string PendingConfirmation = "PendingConfirmation";

    private readonly AwsStorageOptions _options = options.Value;
    private string? _topicArn;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        state.IsInitialized = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureSubscriptionAsync(stoppingToken);
                state.IsInitialized = true;

                logger.LogInformation(
                    "File export SNS subscription is ready. Topic={TopicArn}, Endpoint={Endpoint}",
                    _topicArn,
                    _options.NotificationEndpoint);

                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                state.IsInitialized = false;
                logger.LogWarning(ex, "LocalStack SNS is not ready yet. Retrying subscription...");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionAsync(CancellationToken cancellationToken)
    {
        _topicArn = (await snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _options.TopicName
        }, cancellationToken)).TopicArn;

        var endpoint = _options.NotificationEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(
                "Не задан Aws:NotificationEndpoint — невозможно подписать файловый сервис на SNS.");
        }

        var existing = await FindSubscriptionAsync(endpoint, cancellationToken);

        if (existing is null)
        {
            await snsClient.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = _topicArn,
                Protocol = "http",
                Endpoint = endpoint,
                ReturnSubscriptionArn = true
            }, cancellationToken);
        }

        var confirmedArn = await WaitForConfirmedSubscriptionAsync(endpoint, cancellationToken);

        await snsClient.SetSubscriptionAttributesAsync(new SetSubscriptionAttributesRequest
        {
            SubscriptionArn = confirmedArn,
            AttributeName = "RawMessageDelivery",
            AttributeValue = "true"
        }, cancellationToken);
    }

    private async Task<Subscription?> FindSubscriptionAsync(string endpoint, CancellationToken cancellationToken)
    {
        string? nextToken = null;

        do
        {
            var response = await snsClient.ListSubscriptionsByTopicAsync(
                new ListSubscriptionsByTopicRequest
                {
                    TopicArn = _topicArn,
                    NextToken = nextToken
                },
                cancellationToken);

            var match = response.Subscriptions.FirstOrDefault(s =>
                string.Equals(s.Endpoint, endpoint, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }

            nextToken = response.NextToken;
        }
        while (!string.IsNullOrWhiteSpace(nextToken));

        return null;
    }

    private async Task<string> WaitForConfirmedSubscriptionAsync(string endpoint, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subscription = await FindSubscriptionAsync(endpoint, cancellationToken);

            if (subscription is not null &&
                !string.IsNullOrWhiteSpace(subscription.SubscriptionArn) &&
                !string.Equals(subscription.SubscriptionArn, PendingConfirmation, StringComparison.OrdinalIgnoreCase))
            {
                return subscription.SubscriptionArn;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new InvalidOperationException(
            $"SNS-подписка на {endpoint} не была подтверждена за отведённое время.");
    }
}
