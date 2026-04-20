using Amazon.SQS;
using CreditApp.Domain.Data;
using System.Net;
using System.Text.Json;

namespace CreditApp.Api.Services;

/// <summary>
/// Служба для отправки кредитных заявок в очередь SQS
/// </summary>
public class SqsProducer(IAmazonSQS client, IConfiguration configuration, ILogger<SqsProducer> logger)
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <summary>
    /// Публикует кредитную заявку в очередь SQS
    /// </summary>
    public async Task SendMessage(CreditApplication creditApplication)
    {
        try
        {
            var json = JsonSerializer.Serialize(creditApplication);
            var response = await client.SendMessageAsync(_queueName, json);
            if (response.HttpStatusCode == HttpStatusCode.OK)
                logger.LogInformation("Credit application {id} was sent to sink via SQS", creditApplication.Id);
            else
                throw new Exception($"SQS returned {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send credit application through SQS queue");
        }
    }
}
