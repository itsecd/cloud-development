using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using EmployeeApp.Api.Entities;
using System.Net;
using System.Text.Json;

namespace EmployeeApp.Api.Messaging;

/// <summary>
/// Служба для отправки сообщений в SNS
/// </summary>
/// <param name="client">Клиент SNS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SnsPublisherService(IAmazonSimpleNotificationService client, IConfiguration configuration, ILogger<SnsPublisherService> logger) : IProducerService
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <inheritdoc/>
    public async Task SendMessage(Employee employee)
    {
        try
        {
            var json = JsonSerializer.Serialize(employee);
            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };
            var response = await client.PublishAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Employee {id} was sent to sink via SNS", employee.Id);
            else
                throw new Exception($"SNS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send employee through SNS topic");
        }
    }
}
