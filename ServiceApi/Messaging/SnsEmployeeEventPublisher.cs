using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using Service.Api.Configuration;
using Service.Api.Entities;

namespace Service.Api.Messaging;

/// <summary>
/// Публикует события генерации сотрудников в SNS.
/// </summary>
public sealed class SnsEmployeeEventPublisher(
    IOptions<AwsMessagingOptions> options,
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsEmployeeEventPublisher> logger) : IEmployeeEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly AwsMessagingOptions _options = options.Value;
    private readonly string _replicaId = configuration["ReplicaId"] ?? Environment.MachineName;
    private string? _topicArn;

    public async Task PublishAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= 15; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var topicArn = await EnsureTopicAsync(cancellationToken);

                var payload = JsonSerializer.Serialize(employee, JsonOptions);

                logger.LogInformation(
                    "Publishing employee {EmployeeId} to SNS topic {TopicArn}. Attempt {Attempt}",
                    employee.Id,
                    topicArn,
                    attempt);

                await snsClient.PublishAsync(new PublishRequest
                {
                    TopicArn = topicArn,
                    Subject = $"employee-{employee.Id}",
                    Message = payload,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        ["employeeId"] = new()
                        {
                            DataType = "Number",
                            StringValue = employee.Id.ToString()
                        },
                        ["replicaId"] = new()
                        {
                            DataType = "String",
                            StringValue = _replicaId
                        }
                    }
                }, cancellationToken);

                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _topicArn = null;

                logger.LogWarning(
                    ex,
                    "Failed to publish employee {EmployeeId} to SNS on attempt {Attempt}. Retrying...",
                    employee.Id,
                    attempt);

                if (attempt == 15)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Не удалось опубликовать сотрудника {employee.Id} в SNS после повторных попыток.",
            lastException);
    }

    private async Task<string> EnsureTopicAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_topicArn))
        {
            return _topicArn;
        }

        var response = await snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = _options.TopicName
        }, cancellationToken);

        _topicArn = response.TopicArn;
        return _topicArn;
    }
}
