using Amazon.SQS;
using Patient.Generator.DTO;
using System.Net;
using System.Text.Json;

namespace Patient.Generator.Messaging;

/// <summary>
/// Служба для отправки сообщений с пациентами в SQS
/// </summary>
/// <param name="client">Клиент SQS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public sealed class SqsProducerService(
    IAmazonSQS client,
    IConfiguration configuration,
    ILogger<SqsProducerService> logger) : IProducerService
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc/>
    public async Task SendMessage(PatientDto patient)
    {
        try
        {
            var json = JsonSerializer.Serialize(patient);
            var response = await client.SendMessageAsync(_queueName, json);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Patient {id} was sent to file service via SQS", patient.Id);
            else
                throw new Exception($"SQS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send patient through SQS queue");
        }
    }
}
