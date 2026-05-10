using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace File.Service.Background;

/// <summary>
/// Проверка готовности файлового сервиса к обработке сообщений.
/// </summary>
public sealed class FileExportHealthCheck(FileExportInfrastructureState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            state.IsInitialized
                ? HealthCheckResult.Healthy("Инфраструктура LocalStack инициализирована.")
                : HealthCheckResult.Unhealthy("Файловый сервис ещё не инициализировал SNS/SQS/S3."));
    }
}
