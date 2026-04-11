using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CompanyEmployee.ApiGateway.LoadBalancer;

public class WeightedRoundRobin : ILoadBalancer
{
    private static readonly List<ServiceHostAndPort> _expandedList = [];
    private static int _index = 0;
    private static readonly object _lock = new();
    private static bool _initialized = false;

    public string Type => nameof(WeightedRoundRobin);

    public WeightedRoundRobin(
        List<ServiceHostAndPort> services,
        IConfiguration config)
    {
        if (_initialized) return;

        var weights = config
            .GetSection("LoadBalancerWeights")
            .Get<Dictionary<string, int>>() ?? [];


        foreach (var s in services)
        {

            var key = s.DownstreamPort.ToString();
            var weight = weights.TryGetValue(key, out var w) ? w : 1;

            for (var i = 0; i < weight; i++)
            {
                _expandedList.Add(s);
            }
        }

        _initialized = true;

    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (_lock)
        {
            var s = _expandedList[_index];
            _index = (_index + 1) % _expandedList.Count;

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(s));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}