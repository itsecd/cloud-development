using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CreditApplication.Generator.Models;
using System.Text.Json;

namespace CreditApplication.Generator.Services;

/// <summary>
/// Публикует JSON кредитной заявки в SNS-топик.
/// Топик создаётся лениво при первой публикации.
/// </summary>
public class SnsPublisherService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsPublisherService> logger)
{
    private readonly string _topicName = configuration["AWS:SnsTopicName"] ?? "credit-applications";
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private string? _topicArn;

    public async Task PublishAsync(CreditApplicationModel application, CancellationToken ct = default)
    {
        var arn = _topicArn;
        if (arn is null)
        {
            await _initLock.WaitAsync(ct);
            try
            {
                arn = _topicArn;
                if (arn is null)
                {
                    var response = await snsClient.CreateTopicAsync(_topicName, ct);
                    arn = response.TopicArn;
                    _topicArn = arn;
                    logger.LogInformation("SNS topic resolved: {TopicArn}", arn);
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        var json = JsonSerializer.Serialize(application);

        await snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = arn,
            Message = json
        }, ct);

        logger.LogInformation("Published credit application {Id} to SNS", application.Id);
    }
}
