using Amazon.SQS;
using Amazon.SQS.Model;
using ContractGenerator.Shared.Models;
using System.Net;
using System.Text.Json;

namespace ContractGenerator.Api.Services;

/// <summary>
/// Публикует сгенерированных сотрудников в очередь SQS.
/// </summary>
/// <param name="client">Клиент Amazon SQS.</param>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="logger">Логгер.</param>
public class SqsEmployeePublisher(
    IAmazonSQS client,
    IConfiguration configuration,
    ILogger<SqsEmployeePublisher> logger) : IEmployeePublisher
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    private string? _queueUrl;

    /// <inheritdoc />
    public async Task PublishAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueUrl = await GetQueueUrlAsync(cancellationToken);
            var json = JsonSerializer.Serialize(employee, _jsonOptions);
            var response = await client.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = json
            }, cancellationToken);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"SQS returned {response.HttpStatusCode}");
            }

            logger.LogInformation("Employee {EmployeeId} was sent to SQS queue {QueueName}", employee.Id, _queueName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish employee {EmployeeId} to SQS queue {QueueName}", employee.Id, _queueName);
        }
    }

    private async Task<string> GetQueueUrlAsync(CancellationToken cancellationToken)
    {
        if (_queueUrl is not null)
        {
            return _queueUrl;
        }

        var response = await client.GetQueueUrlAsync(_queueName, cancellationToken);
        _queueUrl = response.QueueUrl;
        return _queueUrl;
    }
}
