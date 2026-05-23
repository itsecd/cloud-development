using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway.LoadBalancers;

public class WeightedRoundRobinLoadBalancer : ILoadBalancer
{
    private readonly List<ServiceHostAndPort> _services;
    private readonly int[] _weights;
    private readonly int _totalWeight;
    private int _counter = -1;
    private readonly object _lock = new();

    public string Type => nameof(WeightedRoundRobinLoadBalancer).Replace("LoadBalancer", "");

    public WeightedRoundRobinLoadBalancer(List<ServiceHostAndPort> services)
    {
        _services = services;
        _weights = new[] { 4, 3, 1 };
        _totalWeight = 0;

        for (var i = 0; i < services.Count; i++)
        {
            _totalWeight += i < _weights.Length ? _weights[i] : 1;
        }
    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (_lock)
        {
            _counter = (_counter + 1) % _totalWeight;

            var cumulative = 0;
            for (var i = 0; i < _services.Count; i++)
            {
                var weight = i < _weights.Length ? _weights[i] : 1;
                cumulative += weight;
                if (_counter < cumulative)
                {
                    var selected = _services[i];
                    Console.WriteLine($"[WRR] counter={_counter} → {selected.DownstreamHost}:{selected.DownstreamPort}");
                    return Task.FromResult<Response<ServiceHostAndPort>>(
                        new OkResponse<ServiceHostAndPort>(selected));
                }
            }
            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(_services[^1]));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
