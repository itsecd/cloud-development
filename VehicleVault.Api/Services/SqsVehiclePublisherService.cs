using Amazon.SQS;
using System.Net;
using System.Text.Json;
using VehicleVault.Api.Entities;

namespace VehicleVault.Api.Services;

/// <summary>
/// Сервис отправки транспортного средства в очередь SQS
/// </summary>
/// <param name="client">Клиент Amazon SQS</param>
/// <param name="configuration">Конфигурация приложения (используется ключ <c>AWS:Resources:SQSQueueName</c>)</param>
/// <param name="logger">Логгер</param>
public class SqsVehiclePublisherService(
    IAmazonSQS client,
    IConfiguration configuration,
    ILogger<SqsVehiclePublisherService> logger) : IVehiclePublisherService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc />
    public async Task Publish(Vehicle vehicle)
    {
        try
        {
            var json = JsonSerializer.Serialize(vehicle);
            var response = await client.SendMessageAsync(_queueName, json);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Vehicle {Id} sent to SQS queue {Queue}", vehicle.SystemId, _queueName);
            else
                throw new InvalidOperationException($"SQS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish vehicle {Id} to SQS", vehicle.SystemId);
        }
    }
}
