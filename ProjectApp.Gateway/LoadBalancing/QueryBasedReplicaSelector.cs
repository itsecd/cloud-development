namespace ProjectApp.Gateway.LoadBalancing;

/// <summary>
/// Вычисляет индекс реплики для Query Based балансировки.
/// </summary>
public static class QueryBasedReplicaSelector
{
    /// <summary>
    /// Возвращает индекс реплики по формуле requestId % replicasCount.
    /// </summary>
    /// <param name="requestId">Значение id из запроса.</param>
    /// <param name="replicasCount">Количество реплик.</param>
    /// <returns>Индекс реплики в диапазоне [0..replicasCount-1].</returns>
    public static int SelectIndex(int requestId, int replicasCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(replicasCount, 0);
        return (int)(((long)requestId % replicasCount + replicasCount) % replicasCount);
    }
}
