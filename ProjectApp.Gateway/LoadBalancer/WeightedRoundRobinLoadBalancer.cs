using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ProjectApp.Gateway.LoadBalancer;

/// <summary>
/// Реализация алгоритма Weighted Round Robin.
/// </summary>
public class WeightedRoundRobinLoadBalancer : ILoadBalancer
{
    private readonly List<ServiceHostAndPort> _services;
    private readonly List<ServiceHostAndPort> _weightedCycle;
    private int _currentIndex = -1;
    private readonly object _lock = new();

    public WeightedRoundRobinLoadBalancer(List<ServiceHostAndPort> services, int[]? weights = null)
    {
        if (services.Count == 0)
        {
            throw new ArgumentException("Services list cannot be empty.", nameof(services));
        }

        _services = services;
        var normalizedWeights = new int[_services.Count];

        if (weights is not null)
        {
            for (var i = 0; i < _services.Count; i++)
            {
                normalizedWeights[i] = i < weights.Length && weights[i] > 0 ? weights[i] : 1;
            }
        }
        else
        {
            Array.Fill(normalizedWeights, 1);
        }

        var totalWeight = normalizedWeights.Sum();
        if (totalWeight <= 0)
        {
            throw new InvalidOperationException("Total weight must be greater than zero.");
        }

        _weightedCycle = [];
        for (var i = 0; i < _services.Count; i++)
        {
            for (var j = 0; j < normalizedWeights[i]; j++)
            {
                _weightedCycle.Add(_services[i]);
            }
        }
    }

    public string Type => nameof(WeightedRoundRobinLoadBalancer).Replace("LoadBalancer", "");

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (_lock)
        {
            _currentIndex = (_currentIndex + 1) % _weightedCycle.Count;

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(_weightedCycle[_currentIndex]));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}
