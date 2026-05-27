using Amazon.SQS.Model;
using ProjectApp.Domain.Events;

namespace ProjectApp.FileService.Messaging;

public sealed class CreditApplicationQueueMessage(Message rawMessage, CreditApplicationGeneratedEvent evt)
{
    public Message RawMessage { get; } = rawMessage;
    public CreditApplicationGeneratedEvent Event { get; } = evt;
}
