using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace FileService.Services;

public class SnsSubscriptionHostedService(
    IAmazonSimpleNotificationService snsClient,
    IS3FileStorageService storageService,
    IConfiguration configuration,
    ILogger<SnsSubscriptionHostedService> logger,
    IHostApplicationLifetime lifetime) : IHostedService
{
    private readonly string _topicName = configuration["AWS:TopicName"] ?? "vehicle-contracts";
    private readonly string? _callbackUrl = configuration["FileService:SnsCallbackUrl"];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await storageService.EnsureBucketExistsAsync();
                    await SubscribeToTopicAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize S3 bucket or SNS subscription");
                }
            }, cancellationToken);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SubscribeToTopicAsync()
    {
        if (string.IsNullOrEmpty(_callbackUrl))
        {
            logger.LogWarning("FileService:SnsCallbackUrl is not configured — skipping SNS subscription");
            return;
        }

        var createResponse = await snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _topicName
        });
        var topicArn = createResponse.TopicArn;

        var endpointUrl = _callbackUrl.TrimEnd('/') + "/sns";

        await snsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "http",
            Endpoint = endpointUrl
        });

        logger.LogInformation("Subscribed to SNS topic {TopicArn} via endpoint {Endpoint}",
            topicArn, endpointUrl);
    }
}
