namespace ProjectApp.FileService.Messaging;

public interface ICreditApplicationQueueConsumer
{
    Task EnsureReadyAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CreditApplicationQueueMessage>> ReceiveAsync(CancellationToken cancellationToken);
    Task DeleteAsync(CreditApplicationQueueMessage message, CancellationToken cancellationToken);
}
