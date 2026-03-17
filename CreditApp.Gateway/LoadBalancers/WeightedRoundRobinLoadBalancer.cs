using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CreditApp.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки, реализующий алгоритм Weighted Round Robin
/// </summary>
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

        var weights = new[] { 3, 2, 1, 1, 1 };
        _sequence = [];

        for (var i = 0; i < services.Count; i++)
        {
            var weight = weights[i];
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