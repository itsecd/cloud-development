using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Vehicle.Api.Entities;

namespace Vehicle.Api.Messaging;

/// <summary>
/// Служба для отправки сообщений в SNS.
/// </summary>
/// <param name="client">Клиент SNS.</param>
/// <param name="options">Настройки SNS.</param>
/// <param name="logger">Логгер.</param>
public class SnsPublisherService(IAmazonSimpleNotificationService client, IOptions<SnsOptions> options, ILogger<SnsPublisherService> logger) : IProducerService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _topicArn = !string.IsNullOrWhiteSpace(options.Value.TopicArn)
        ? options.Value.TopicArn
        : throw new KeyNotFoundException("SNS topic link was not found in configuration");

    /// <inheritdoc/>
    public async Task SendMessageAsync(VehicleEntity vehicle, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(vehicle, _jsonOptions);

            var request = new PublishRequest
            {
                Message = json,
                TopicArn = _topicArn
            };

            var response = await client.PublishAsync(request, cancellationToken);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("Vehicle {VehicleId} was sent to file service via SNS", vehicle.Id);
            }
            else
            {
                throw new Exception($"SNS returned {response.HttpStatusCode}");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unable to send vehicle through SNS topic");
        }
    }
}