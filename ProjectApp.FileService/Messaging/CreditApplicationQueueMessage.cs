using Amazon.SQS.Model;
using ProjectApp.Domain.Messaging;

namespace ProjectApp.FileService.Messaging;

public sealed class CreditApplicationQueueMessage
{
    public required Message RawMessage { get; init; }

    public required CreditApplicationGeneratedEvent Event { get; init; }
}
