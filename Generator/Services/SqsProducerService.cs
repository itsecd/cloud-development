using Amazon.SQS;
using Service.Api.Dto;
using System.Net;
using System.Text.Json;

namespace Service.Api.Services;

/// <summary>
/// Служба для отправки сообщений в SQS
/// </summary>
/// <param name="client">Клиент SQS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SqsProducerService(IAmazonSQS client, IConfiguration configuration, ILogger<SqsProducerService> logger)
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue link was not found in configuration");

    ///<inheritdoc/>
    public async Task SendMessage(CreditOrderDto creditOrder)
    {
        try
        {
            var json = JsonSerializer.Serialize(creditOrder);

            var queueUrlResponse = await client.GetQueueUrlAsync(_queueName);
            var queueUrl = queueUrlResponse.QueueUrl;

            var responce = await client.SendMessageAsync(queueUrl, json);
            if (responce.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Land plot {id} was sent to sink via SQS", creditOrder.Id);
            else
                throw new Exception($"SQS returned {responce.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send cridit order through SQS queue");
        }
    }
}
