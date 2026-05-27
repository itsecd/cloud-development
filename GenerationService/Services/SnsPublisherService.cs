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
            Amazon.SimpleNotificationService.Model.Topic? topic = null;

            // Ждём пока топик появится (до 30 секунд)
            for (var i = 0; i < 6; i++)
            {
                topic = await snsClient.FindTopicAsync(TopicName);
                if (topic != null) break;
                logger.LogWarning("SNS Topic '{Topic}' не найден, повтор через 5с...", TopicName);
                await Task.Delay(5000, ct);
            }

            if (topic == null)
            {
                logger.LogWarning("SNS Topic '{Topic}' не найден после ожидания", TopicName);
                return;
            }

            var json = JsonSerializer.Serialize(contract);
            await snsClient.PublishAsync(new PublishRequest
            {
                TopicArn = topic.TopicArn,
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