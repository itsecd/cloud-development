using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway.LoadBalancing;

public sealed class WeightedRandomLoadBalancer : ILoadBalancer
{
    public string Type => "WeightedRandom";

    private readonly IReadOnlyList<(ServiceHostAndPort Host, double CumulativeWeight)> _entries;

    public WeightedRandomLoadBalancer(
        IReadOnlyList<ServiceHostAndPort> hosts,
        IReadOnlyList<double> weights)
    {
        if (hosts.Count == 0)
            throw new ArgumentException("Список хостов не может быть пустым.", nameof(hosts));

        if (hosts.Count != weights.Count)
            throw new ArgumentException(
                $"Число хостов ({hosts.Count}) должно совпадать с числом весов ({weights.Count}).");

        var sum = weights.Sum();
        if (Math.Abs(sum - 1.0) > 1e-9)
            throw new ArgumentException(
                $"Сумма весов должна равняться 1. Текущая сумма: {sum:F6}");

        var cumulative = 0.0;
        var entries = new List<(ServiceHostAndPort, double)>(hosts.Count);
        for (var i = 0; i < hosts.Count; i++)
        {
            cumulative += weights[i];
            entries.Add((hosts[i], cumulative));
        }
        _entries = entries;
    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var roll = Random.Shared.NextDouble();

        foreach (var (host, cumulativeWeight) in _entries)
        {
            if (roll < cumulativeWeight)
                return Task.FromResult<Response<ServiceHostAndPort>>(
                    new OkResponse<ServiceHostAndPort>(host));
        }

        return Task.FromResult<Response<ServiceHostAndPort>>(
            new OkResponse<ServiceHostAndPort>(_entries[^1].Host));
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}