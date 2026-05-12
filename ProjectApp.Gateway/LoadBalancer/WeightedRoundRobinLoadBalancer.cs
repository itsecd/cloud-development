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
    private readonly int[] _weights;
    private readonly int[] _currentWeights;
    private readonly int _totalWeight;
    private readonly object _lock = new();

    public WeightedRoundRobinLoadBalancer(List<ServiceHostAndPort> services, int[]? weights = null)
    {
        if (services.Count == 0)
        {
            throw new ArgumentException("Services list cannot be empty.", nameof(services));
        }

        _services = services;
        _weights = new int[_services.Count];

        if (weights is not null)
        {
            for (var i = 0; i < _services.Count; i++)
            {
                _weights[i] = i < weights.Length && weights[i] > 0 ? weights[i] : 1;
            }
        }
        else
        {
            Array.Fill(_weights, 1);
        }

        _totalWeight = _weights.Sum();
        if (_totalWeight <= 0)
        {
            throw new InvalidOperationException("Total weight must be greater than zero.");
        }

        _currentWeights = new int[_services.Count];
        Array.Copy(_weights, _currentWeights, _services.Count);
    }

    public string Type => nameof(WeightedRoundRobinLoadBalancer).Replace("LoadBalancer", "");

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (_lock)
        {
            var maxIndex = 0;
            var maxWeight = _currentWeights[0];

            for (var i = 1; i < _services.Count; i++)
            {
                if (_currentWeights[i] > maxWeight)
                {
                    maxWeight = _currentWeights[i];
                    maxIndex = i;
                }
            }

            _currentWeights[maxIndex] -= _totalWeight;

            for (var i = 0; i < _services.Count; i++)
            {
                _currentWeights[i] += _weights[i];
            }

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(_services[maxIndex]));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}
