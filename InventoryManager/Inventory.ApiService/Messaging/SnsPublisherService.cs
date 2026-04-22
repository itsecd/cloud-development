using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Inventory.ApiService.Entity;
using System.Net;
using System.Text.Json;

namespace Inventory.ApiService.Messaging;

public class SnsPublisherService(
    IAmazonSimpleNotificationService client,
    IConfiguration configuration,
    ILogger<SnsPublisherService> logger) : IProducerService
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic link was not found in configuration");

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