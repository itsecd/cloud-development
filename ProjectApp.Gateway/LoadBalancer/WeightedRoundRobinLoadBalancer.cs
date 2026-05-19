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
    private int _currentIndex = -1;
    private int _remainingSelections;
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

        var totalWeight = _weights.Sum();
        if (totalWeight <= 0)
        {
            throw new InvalidOperationException("Total weight must be greater than zero.");
        }

        MoveToNextService();
    }

    public string Type => nameof(WeightedRoundRobinLoadBalancer).Replace("LoadBalancer", "");

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (_lock)
        {
            var selectedService = _services[_currentIndex];
            _remainingSelections--;

            if (_remainingSelections == 0)
            {
                MoveToNextService();
            }

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(selectedService));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }

    private void MoveToNextService()
    {
        _currentIndex = (_currentIndex + 1) % _services.Count;
        _remainingSelections = _weights[_currentIndex];
    }
}
