namespace ProjectApp.FileService.Messaging;

public interface ICreditApplicationEventConsumer
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CreditApplicationQueueMessage>> ReceiveAsync(CancellationToken cancellationToken);

    Task DeleteAsync(CreditApplicationQueueMessage message, CancellationToken cancellationToken);
}
