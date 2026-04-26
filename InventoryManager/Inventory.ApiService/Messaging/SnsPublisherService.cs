using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Inventory.ApiService.Entity;
using System.Net;
using System.Text.Json;

namespace Inventory.ApiService.Messaging;

/// <summary>
/// Реализация <see cref="IProducerService"/> для отправки сообщений в Amazon SNS (Simple Notification Service). 
/// Сериализует продукт в JSON и публикует его в указанный SNS-топик.
/// </summary>
public class SnsPublisherService(IAmazonSimpleNotificationService client, IConfiguration configuration, ILogger<SnsPublisherService> logger) : IProducerService
{
    /// <summary>
    /// ARN (Amazon Resource Name) SNS-топика, полученный из конфигурации приложения.
    /// </summary>
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <summary>
    /// Асинхронно отправляет сериализованный в JSON продукт в SNS-топик.В случае успешной отправки (HTTP 200) логирует информацию.
    /// При ошибке логирует исключение, но не выбрасывает его повторно.
    /// </summary>
    /// <param name="product">Продукт, который необходимо отправить.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task SendMessage(Product product)
    {
        try
        {
            var json = JsonSerializer.Serialize(product);

            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };

            var response = await client.PublishAsync(request);

            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Inventory {id} was sent to sink via SNS", product.Id);
            else
                throw new Exception($"SNS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send inventory through SNS topic");
        }
    }
}