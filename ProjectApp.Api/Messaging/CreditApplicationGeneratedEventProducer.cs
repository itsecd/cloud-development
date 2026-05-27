using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ProjectApp.Domain.Entities;
using ProjectApp.Domain.Events;

namespace ProjectApp.Api.Messaging;

public class CreditApplicationGeneratedEventProducer(
    IAmazonSQS sqs,
    IConfiguration configuration,
    ILogger<CreditApplicationGeneratedEventProducer> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string? _queueUrl;

    public async Task ProduceAsync(CreditApplication application, CancellationToken cancellationToken)
    {
        var queueUrl = await GetQueueUrlAsync(cancellationToken);
        var message = new CreditApplicationGeneratedEvent
        {
            Id = application.Id,
            OccurredAtUtc = DateTime.UtcNow,
            Application = application
        };

        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message, JsonOptions)
        }, cancellationToken);

        logger.LogInformation("Credit application {Id} sent to SQS", application.Id);
    }

    private async Task<string> GetQueueUrlAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_queueUrl))
        {
            return _queueUrl;
        }

        var queueName = configuration["Sqs:QueueName"] ?? "credit-application-generated";
        var response = await sqs.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        }, cancellationToken);

        _queueUrl = response.QueueUrl;
        return _queueUrl;
    }
}
