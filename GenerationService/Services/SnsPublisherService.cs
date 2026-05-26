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
            var json = JsonSerializer.Serialize(contract);

            var request = new PublishRequest
            {
                TopicArn = TopicName,
                Message = json
            };

            await snsClient.PublishAsync(request, ct);
            logger.LogInformation("✅ Опубликовано в SNS: контракт {Id}", contract.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось опубликовать контракт {Id} в SNS", contract.Id);
        }
    }
}