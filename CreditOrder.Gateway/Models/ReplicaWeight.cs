namespace Api.Gateway.Models;

/// <summary>
/// Модель конфигурации реплики downstream-сервиса, используемая балансировщиком нагрузки шлюза.
/// </summary>
public class ReplicaWeight
{
    /// <summary>
    /// Хост сервиса-реплики.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    /// <summary>
    /// Порт, на котором работает реплика сервиса.
    /// </summary>
    public int Port { get; set; }
    /// <summary>
    /// Вес реплики для алгоритма балансировки нагрузки.
    /// Чем больше значение, тем чаще на эту реплику будут направляться запросы.
    /// </summary>
    public int Weight { get; set; }
}
