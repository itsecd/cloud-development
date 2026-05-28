using Amazon.SQS;
using System.Net;
using System.Text.Json;
using VehicleGen.Api.Entities;

namespace VehicleGen.Api.Services;

/// <summary>
/// Сервис отправки транспортных средств в очередь SQS
/// </summary>
public class SqsVehiclePublisherService : IVehiclePublisherService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueName;
    private readonly ILogger<SqsVehiclePublisherService> _logger;
    private string? _queueUrl;

    /// <summary>
    /// Конструктор сервиса публикации
    /// </summary>
    /// <param name="sqsClient">Клиент SQS</param>
    /// <param name="configuration">Конфигурация приложения</param>
    /// <param name="logger">Логгер</param>
    public SqsVehiclePublisherService(
        IAmazonSQS sqsClient,
        IConfiguration configuration,
        ILogger<SqsVehiclePublisherService> logger)
    {
        _sqsClient = sqsClient;
        _queueName = configuration["AWS:Resources:SQSQueueName"]
            ?? throw new InvalidOperationException("SQS queue name not configured");
        _logger = logger;
    }

    public async Task SendVehicleToQueueAsync(Vehicle vehicle)
    {
        try
        {
            if (_queueUrl == null)
            {
                var getUrlResponse = await _sqsClient.GetQueueUrlAsync(_queueName);
                _queueUrl = getUrlResponse.QueueUrl;
            }

            var json = JsonSerializer.Serialize(vehicle);
            var sendResponse = await _sqsClient.SendMessageAsync(_queueUrl, json);

            if (sendResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Vehicle {Id} sent to queue {QueueName}", vehicle.Id, _queueName);
            }
            else
            {
                _logger.LogWarning("Failed to send vehicle {Id}, HTTP status: {Status}", vehicle.Id, sendResponse.HttpStatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending vehicle {Id} to SQS", vehicle.Id);
        }
    }
}