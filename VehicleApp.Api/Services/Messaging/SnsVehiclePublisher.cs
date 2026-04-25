using System.Net;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services.Messaging;

/// <summary>
/// Реализация <see cref="IVehiclePublisher"/> через Amazon SNS.
/// ТС сериализуется в JSON и отправляется в топик, ARN которого берётся из конфигурации
/// </summary>
/// <param name="client">SNS-клиент AWS SDK (под LocalStack указывает на контейнер)</param>
/// <param name="configuration">Конфигурация с ключом <c>AWS:Resources:SNSTopicArn</c></param>
/// <param name="logger">Логгер для результата публикации</param>
public class SnsVehiclePublisher(
    IAmazonSimpleNotificationService client,
    IConfiguration configuration,
    ILogger<SnsVehiclePublisher> logger) : IVehiclePublisher
{
    private readonly string _topicArn = configuration["AWS:Resources:SNSTopicArn"]
        ?? throw new KeyNotFoundException("SNS topic ARN was not found in configuration");

    /// <inheritdoc />
    public async Task Publish(Vehicle vehicle)
    {
        try
        {
            var payload = JsonSerializer.Serialize(vehicle);
            var request = new PublishRequest { TopicArn = _topicArn, Message = payload };
            var response = await client.PublishAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Vehicle {id} published to SNS topic", vehicle.Id);
            else
                logger.LogError("Failed to publish vehicle {id}: {code}", vehicle.Id, response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while publishing vehicle {id} to SNS", vehicle.Id);
        }
    }
}
