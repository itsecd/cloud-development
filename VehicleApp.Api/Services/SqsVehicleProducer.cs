using System.Net;
using System.Text.Json;
using Amazon.SQS;
using VehicleApp.Api.Models;

namespace VehicleApp.Api.Services;

/// <summary>
/// Отправляет сериализованное транспортное средство в очередь SQS
/// </summary>
/// <param name="client">Клиент SQS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public sealed class SqsVehicleProducer(IAmazonSQS client, IConfiguration configuration, ILogger<SqsVehicleProducer> logger) : IVehicleProducer
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc />
    public async Task SendAsync(Vehicle vehicle)
    {
        try
        {
            var json = JsonSerializer.Serialize(vehicle, _serializerOptions);
            var response = await client.SendMessageAsync(_queueName, json);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Vehicle {id} was sent to SQS queue {queue}", vehicle.Id, _queueName);
            else
                throw new Exception($"SQS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send vehicle {id} to SQS queue", vehicle.Id);
        }
    }
}
