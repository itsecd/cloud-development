using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using ProjectApp.Domain.Messaging;
using ProjectApp.FileService.Options;

namespace ProjectApp.FileService.Messaging;

public class SqsCreditApplicationEventConsumer(
    IAmazonSQS sqsClient,
    IOptions<FilePersistenceOptions> options,
    ILogger<SqsCreditApplicationEventConsumer> logger) : ICreditApplicationEventConsumer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string? _queueUrl;

    public async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        _queueUrl ??= await EnsureQueueAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditApplicationQueueMessage>> ReceiveAsync(CancellationToken cancellationToken)
    {
        await EnsureReadyAsync(cancellationToken);

        var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 5,
            WaitTimeSeconds = 10
        }, cancellationToken);

        var messages = new List<CreditApplicationQueueMessage>();
        foreach (var message in response.Messages ?? [])
        {
            var evt = JsonSerializer.Deserialize<CreditApplicationGeneratedEvent>(message.Body, JsonOptions);
            if (evt is null)
            {
                logger.LogWarning("Message {MessageId} cannot be deserialized and will be removed", message.MessageId);
                await sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, cancellationToken);
                continue;
            }

            messages.Add(new CreditApplicationQueueMessage
            {
                RawMessage = message,
                Event = evt
            });
        }

        return messages;
    }

    public async Task DeleteAsync(CreditApplicationQueueMessage message, CancellationToken cancellationToken)
    {
        if (_queueUrl is null)
        {
            return;
        }

        await sqsClient.DeleteMessageAsync(_queueUrl, message.RawMessage.ReceiptHandle, cancellationToken);
    }

    private async Task<string> EnsureQueueAsync(CancellationToken cancellationToken)
    {
        var response = await sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = options.Value.QueueName
        }, cancellationToken);

        return response.QueueUrl;
    }
}
