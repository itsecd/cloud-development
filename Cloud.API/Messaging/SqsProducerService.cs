using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Cloud.Api.Models;

namespace Cloud.Api.Messaging;

/// <summary>
/// Сервис отправки сгенерированного сотрудника в очередь SQS
/// </summary>
public class SqsProducerService : IProducerService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<SqsProducerService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public SqsProducerService(IAmazonSQS sqsClient, IConfiguration configuration, ILogger<SqsProducerService> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _queueUrl = configuration["AWS:Resources:SQSQueueUrl"]
                    ?? throw new KeyNotFoundException("SQS queue URL not found in configuration.");
    }

    /// <inheritdoc />
    public async Task SendMessage(Employee employee)
    {
        var json = JsonSerializer.Serialize(employee, _jsonOptions);
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = json
        };

        try
        {
            var response = await _sqsClient.SendMessageAsync(request);
            _logger.LogInformation("Sent message for Employee {Id}, MessageId {MessageId}", employee.Id, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message for Employee {Id}", employee.Id);
            throw;
        }
    }
}
