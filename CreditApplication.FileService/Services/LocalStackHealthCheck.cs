using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditApplication.FileService.Services;

/// <summary>
/// Readiness-проверка: убеждается, что конкретная SQS-очередь существует и доступна.
/// </summary>
public class LocalStackHealthCheck(
    IAmazonSQS sqsClient,
    IConfiguration configuration) : IHealthCheck
{
    private readonly string _queueName =
        configuration["AWS:SqsQueueName"] ?? "credit-applications-file-queue";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await sqsClient.GetQueueUrlAsync(_queueName, cancellationToken);
            return HealthCheckResult.Healthy($"SQS queue '{_queueName}' is accessible");
        }
        catch (QueueDoesNotExistException)
        {
            return HealthCheckResult.Unhealthy($"SQS queue '{_queueName}' does not exist");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQS is not reachable", ex);
        }
    }
}
