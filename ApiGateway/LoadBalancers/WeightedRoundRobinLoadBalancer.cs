using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway.LoadBalancers;

public class WeightedRoundRobinLoadBalancer : ILoadBalancer
{
    private readonly List<ServiceHostAndPort> _sequence;
    private int _index = -1;
    private readonly object _lock = new();
    private readonly string _type;

    public string Type => _type;

    public WeightedRoundRobinLoadBalancer(List<ServiceHostAndPort> services)
    {
        _type = nameof(WeightedRoundRobinLoadBalancer).Replace("LoadBalancer", "");

        var weights = new[] { 4, 3, 1 };
        _sequence = new List<ServiceHostAndPort>();

        for (var i = 0; i < services.Count; i++)
        {
            var weight = i < weights.Length ? weights[i] : 1;
            for (var j = 0; j < weight; j++)
            {
                _sequence.Add(services[i]);
            }
        }
    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (_lock)
        {
            _index = (_index + 1) % _sequence.Count;
            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(_sequence[_index]));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}