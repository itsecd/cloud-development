using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CreditApp.Api.Models;
using System.Net;
using System.Text.Json;

namespace CreditApp.Api.Messaging;

/// <summary>
/// Служба для отправки сообщений в SNS
/// </summary>
/// <param name="client">Клиент SNS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SnsPublisherService(
    IAmazonSimpleNotificationService client,
    IConfiguration configuration,
    ILogger<SnsPublisherService> logger) : IProducerService
{
    /// <summary>
    /// Уникальный идентификатор топика SNS
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");

    /// <inheritdoc/>
    public async Task SendMessage(CreditApplication application)
    {
        try
        {
            var json = JsonSerializer.Serialize(application);
            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };
            var response = await client.PublishAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Credit application {Id} sent to SNS", application.Id);
            else
                throw new Exception($"SNS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send credit application {Id} through SNS", application.Id);
        }
    }
}
