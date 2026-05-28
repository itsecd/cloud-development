using Amazon.SQS;
using Amazon.SQS.Model;
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
    private readonly SemaphoreSlim _lock = new(1, 1);

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
        _logger = logger;

        _queueName = configuration["AWS:Resources:SQSQueueName"]
            ?? throw new InvalidOperationException("SQS queue name not configured");
    }

    private async Task EnsureQueueAsync()
    {
        if (_queueUrl != null)
            return;

        await _lock.WaitAsync();
        try
        {
            if (_queueUrl != null)
                return;

            try
            {
                var response = await _sqsClient.GetQueueUrlAsync(_queueName);
                _queueUrl = response.QueueUrl;
            }
            catch (Amazon.SQS.Model.QueueDoesNotExistException)
            {
                var create = await _sqsClient.CreateQueueAsync(_queueName);
                _queueUrl = create.QueueUrl;
            }

            _logger.LogInformation("Queue ready: {QueueUrl}", _queueUrl);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SendVehicleToQueueAsync(Vehicle vehicle)
    {
        try
        {
            await EnsureQueueAsync();

            var json = JsonSerializer.Serialize(vehicle);

            var sendResponse = await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl!,
                MessageBody = json
            });

            if (sendResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation(
                    "Vehicle {Id} sent to queue {QueueName}",
                    vehicle.Id,
                    _queueName);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send vehicle {Id}, status: {Status}",
                    vehicle.Id,
                    sendResponse.HttpStatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending vehicle {Id} to SQS", vehicle.Id);
        }
    }
}