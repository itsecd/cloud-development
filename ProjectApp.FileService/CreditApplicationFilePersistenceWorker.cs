using ProjectApp.FileService.Messaging;
using ProjectApp.FileService.Storage;

namespace ProjectApp.FileService;

public class CreditApplicationFilePersistenceWorker(
    ICreditApplicationQueueConsumer queueConsumer,
    ICreditApplicationObjectStorage objectStorage,
    ILogger<CreditApplicationFilePersistenceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForInfrastructureAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var message in await queueConsumer.ReceiveAsync(stoppingToken))
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to receive or process SQS messages");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(CreditApplicationQueueMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await objectStorage.SaveAsync(message.Event, cancellationToken);
            await queueConsumer.DeleteAsync(message, cancellationToken);
            logger.LogInformation("Credit application {Id} saved to Minio", message.Event.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process SQS message {MessageId}", message.RawMessage.MessageId);
        }
    }

    private async Task WaitForInfrastructureAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                await queueConsumer.EnsureReadyAsync(cancellationToken);
                await objectStorage.EnsureBucketAsync(cancellationToken);
                logger.LogInformation("SQS queue and Minio bucket are ready");
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested && attempt < 30)
            {
                attempt++;
                logger.LogWarning(ex, "External dependency is not ready yet, retrying attempt {Attempt}", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
