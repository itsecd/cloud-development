namespace ApiGateway.Configuration;

public sealed class WeightedRoundRobinOptions
{
    public const string SectionName = "WeightedRoundRobin";

    public List<ReplicaNodeOptions> Nodes { get; init; } = new();
}

public sealed class ReplicaNodeOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public int Weight { get; init; } = 1;
    public string ReplicaId { get; init; } = string.Empty;
}
