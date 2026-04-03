using Amazon.SQS;
using Amazon.SQS.Model;
using CreditApp.Messaging.Contracts;
using System.Text.Json;

namespace CreditApp.Api.Services;

/// <summary>
/// Продюсер для отправки сообщений в очередь SQS (LocalStack)
/// </summary>
public class SqsProducer
{
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<SqsProducer> _logger;
    private readonly string _queueUrl = "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/credit-queue";

    public SqsProducer(IAmazonSQS sqs, ILogger<SqsProducer> logger)
    {
        _sqs = sqs;
        _logger = logger;
    }

    /// <summary>
    /// Публикует событие о сгенерированной кредитной заявке в очередь SQS
    /// </summary>
    public async Task PublishAsync(CreditGeneratedEvent creditEvent)
    {
        var message = JsonSerializer.Serialize(creditEvent);
        var request = new SendMessageRequest { QueueUrl = _queueUrl, MessageBody = message };
        await _sqs.SendMessageAsync(request);
        _logger.LogInformation("Published credit {Id} to SQS", creditEvent.Id);
    }
}