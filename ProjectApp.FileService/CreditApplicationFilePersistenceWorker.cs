using ProjectApp.FileService.Messaging;
using ProjectApp.FileService.Storage;

namespace ProjectApp.FileService;

public class CreditApplicationFilePersistenceWorker(
    ICreditApplicationEventConsumer eventConsumer,
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
                foreach (var message in await eventConsumer.ReceiveAsync(stoppingToken))
                {
                    await HandleMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SQS is unavailable, retrying message polling");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(CreditApplicationQueueMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await objectStorage.SaveAsync(message.Event, cancellationToken);
            await eventConsumer.DeleteAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process SQS message {MessageId}", message.RawMessage.MessageId);
        }
    }

    private async Task WaitForInfrastructureAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await eventConsumer.EnsureReadyAsync(cancellationToken);
                await objectStorage.EnsureBucketAsync(cancellationToken);
                logger.LogInformation("SQS queue and S3 bucket are ready");
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SQS/S3 infrastructure is unavailable, retrying");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

}
