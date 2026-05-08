using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Cloud.Api.Models;

namespace Cloud.Api.Messaging;

/// <summary>
/// Сервис отправки сгенерированного сотрудника в очередь SQS
/// </summary>
/// <param name="sqsClient">Клиент SQS</param>
/// <param name="configuration">Конфигурация приложения</param>
/// <param name="logger">Логгер</param>
public class SqsProducerService(
    IAmazonSQS sqsClient, 
    IConfiguration configuration, 
    ILogger<SqsProducerService> logger
    ) : IProducerService
{
    private readonly string _queueUrl = configuration["AWS:Resources:SQSQueueUrl"]
        ?? throw new KeyNotFoundException("SQS queue URL not found in configuration.");

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

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
            var response = await sqsClient.SendMessageAsync(request);
            logger.LogInformation("Sent message for Employee {Id}, MessageId {MessageId}", employee.Id, response.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message for Employee {Id}", employee.Id);
            throw;
        }
    }
}
