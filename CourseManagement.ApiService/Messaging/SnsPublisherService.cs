using System.Net;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace CourseManagement.ApiService.Messaging;

/// <summary>
/// Универсальный сервис для отправки в брокер сгенерированных сущностей
/// </summary>
/// <param name="logger"></param>
/// <param name="client"></param>
/// <param name="configuration"></param>
public class SnsPublisherService(ILogger<SnsPublisherService> logger, IAmazonSimpleNotificationService client, IConfiguration configuration) : IPublisherService
{
    /// <summary>
    /// Идентификатор топика
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <inheritdoc/>
    public async Task<bool> SendMessage(int id, object entity, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(entity);
            
            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn,
            };
            var response = await client.PublishAsync(request, cancellationToken);
            
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("{EntityType} {Id} was sent to sink via SNS", entity.GetType(), id);
                return true;
            }

            logger.LogWarning("SNS returned {StatusCode} for {EntityType} {Id}", response.HttpStatusCode, entity.GetType(), id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send {EntityType} through SNS topic", entity.GetType());
        }

        return false;
    }
}
