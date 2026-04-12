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
public class SnsPublisherService<T>(ILogger<SnsPublisherService<T>> logger, IAmazonSimpleNotificationService client, IConfiguration configuration) : IPublisherService<T>
{
    /// <summary>
    /// Идентификатор топика
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <inheritdoc/>
    public async Task<bool> SendMessage(int id, T entity, CancellationToken cancellationToken)
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
                logger.LogInformation("{EntityType} {Id} was sent to sink via SNS", typeof(T).Name, id);
                return true;
            }

            logger.LogWarning("SNS returned {StatusCode} for {EntityType} {Id}", response.HttpStatusCode, typeof(T).Name, id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send {EntityType} through SNS topic", typeof(T).Name);
        }

        return false;
    }
}
