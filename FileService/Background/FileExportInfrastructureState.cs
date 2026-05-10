namespace File.Service.Background;

/// <summary>
/// Состояние инициализации инфраструктуры файлового сервиса.
/// </summary>
public sealed class FileExportInfrastructureState
{
    /// <summary>
    /// Признак готовности инфраструктуры SNS/SQS/S3.
    /// </summary>
    public bool IsInitialized { get; set; }
}
