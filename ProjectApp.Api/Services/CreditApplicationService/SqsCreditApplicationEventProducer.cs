using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using ProjectApp.Api.Options;
using ProjectApp.Domain.Entities;
using ProjectApp.Domain.Messaging;

namespace ProjectApp.Api.Services.CreditApplicationService;

/// <summary>
/// Отправляет события о сгенерированных заявках в SQS.
/// </summary>
public class SqsCreditApplicationEventProducer(
    IAmazonSQS sqsClient,
    IOptions<AwsMessagingOptions> options,
    ILogger<SqsCreditApplicationEventProducer> logger) : ICreditApplicationEventProducer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private string? _queueUrl;

    public async Task ProduceGeneratedAsync(CreditApplication application, CancellationToken cancellationToken)
    {
        _queueUrl ??= await EnsureQueueAsync(cancellationToken);

        var message = new CreditApplicationGeneratedEvent
        {
            Id = application.Id,
            OccurredAtUtc = DateTime.UtcNow,
            Application = application
        };

        var payload = JsonSerializer.Serialize(message, JsonOptions);
        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = payload
        }, cancellationToken);

        logger.LogInformation("Produced credit application generated event for Id={Id} to queue {Queue}", application.Id, options.Value.QueueName);
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
