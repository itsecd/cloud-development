using ProjectApp.Domain.Messaging;

namespace ProjectApp.FileService.Storage;

public interface ICreditApplicationObjectStorage
{
    static string BuildObjectKey(int id) => $"credit-applications/{id}.json";

    Task EnsureBucketAsync(CancellationToken cancellationToken);

    Task SaveAsync(CreditApplicationGeneratedEvent evt, CancellationToken cancellationToken);
}
