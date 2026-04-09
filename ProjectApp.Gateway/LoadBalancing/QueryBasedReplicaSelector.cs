namespace ProjectApp.Gateway.LoadBalancing;

public static class QueryBasedReplicaSelector
{
    public static int SelectIndex(int requestId, int replicasCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(replicasCount, 0);
        return (int)(((long)requestId % replicasCount + replicasCount) % replicasCount);
    }
}
