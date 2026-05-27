using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using GenerationService.Models;
using System.Text.Json;

namespace GenerationService.Services;

public class SnsPublisherService(
    IAmazonSimpleNotificationService snsClient,
    ILogger<SnsPublisherService> logger)
{
    private const string TopicName = "software-projects-topic";

    public async Task PublishAsync(SoftwareProjectContract contract, CancellationToken ct = default)
    {
        try
        {
            // Получаем реальный ARN топика
            var topicResponse = await snsClient.FindTopicAsync(TopicName);
            if (topicResponse == null)
            {
                logger.LogWarning("SNS Topic '{Topic}' не найден", TopicName);
                return;
            }

            var json = JsonSerializer.Serialize(contract);
            await snsClient.PublishAsync(new PublishRequest
            {
                TopicArn = topicResponse.TopicArn,
                Message = json
            }, ct);

            logger.LogInformation("✅ Опубликовано в SNS: контракт {Id}", contract.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось опубликовать контракт {Id} в SNS", contract.Id);
        }
    }
}