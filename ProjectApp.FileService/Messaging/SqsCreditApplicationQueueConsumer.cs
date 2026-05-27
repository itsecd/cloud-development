using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ProjectApp.Domain.Events;

namespace ProjectApp.FileService.Messaging;

public class SqsCreditApplicationQueueConsumer(
    IAmazonSQS sqs,
    IConfiguration configuration,
    ILogger<SqsCreditApplicationQueueConsumer> logger) : ICreditApplicationQueueConsumer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string? _queueUrl;

    public async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        _queueUrl = await EnsureQueueExistsAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CreditApplicationQueueMessage>> ReceiveAsync(CancellationToken cancellationToken)
    {
        var queueUrl = await GetQueueUrlAsync(cancellationToken);
        var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 5
        }, cancellationToken);

        var messages = new List<CreditApplicationQueueMessage>();
        foreach (var message in response.Messages ?? [])
        {
            var generatedEvent = JsonSerializer.Deserialize<CreditApplicationGeneratedEvent>(message.Body, JsonOptions);
            if (generatedEvent?.Application is null)
            {
                logger.LogWarning("Received invalid SQS message {MessageId}", message.MessageId);
                await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                continue;
            }

            messages.Add(new CreditApplicationQueueMessage(message, generatedEvent));
        }

        return messages;
    }

    public async Task DeleteAsync(CreditApplicationQueueMessage message, CancellationToken cancellationToken)
    {
        var queueUrl = await GetQueueUrlAsync(cancellationToken);
        await sqs.DeleteMessageAsync(queueUrl, message.RawMessage.ReceiptHandle, cancellationToken);
    }

    private async Task<string> GetQueueUrlAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_queueUrl))
        {
            return _queueUrl;
        }

        return await EnsureQueueExistsAsync(cancellationToken);
    }

    private async Task<string> EnsureQueueExistsAsync(CancellationToken cancellationToken)
    {
        var queueName = configuration["Sqs:QueueName"] ?? "credit-application-generated";
        var response = await sqs.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        }, cancellationToken);

        _queueUrl = response.QueueUrl;
        return _queueUrl;
    }
}
