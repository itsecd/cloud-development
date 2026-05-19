namespace ProjectApp.Gateway.Configuration;

/// <summary>
/// Настройки весов для балансировщика Weighted Round Robin.
/// </summary>
public class WeightedRoundRobinOptions
{
    public const string SectionName = "WeightedRoundRobin";

    public int[] Weights { get; set; } = [];
}
