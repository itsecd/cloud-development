using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CompanyEmployee.ApiGateway.LoadBalancer;

public class WeightedRoundRobin : ILoadBalancer
{
    private static List<ServiceHostAndPort> _services = [];
    private static List<int> _weights = [];
    private static int _totalWeight = 0;

    private static int _index = 0;
    private static readonly object _lock = new();
    private static bool _initialized = false;

    public string Type => nameof(WeightedRoundRobin);

    public WeightedRoundRobin(
        List<ServiceHostAndPort> services,
        IConfiguration config)
    {
        if (_initialized) return;

        var weightsConfig = config
            .GetSection("LoadBalancerWeights")
            .Get<Dictionary<string, int>>() ?? [];

        for (var i = 0; i < services.Count; i++)
        {
            var key = $"R{i + 1}";
            var weight = weightsConfig.TryGetValue(key, out var w) ? w : 1;

            _services.Add(services[i]);
            _weights.Add(weight);
            _totalWeight += weight;
        }

        _initialized = true;
    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (_lock)
        {
            var current = _index % _totalWeight;
            _index++;

            var sum = 0;
            for (var i = 0; i < _services.Count; i++)
            {
                sum += _weights[i];
                if (current < sum)
                {
                    return Task.FromResult<Response<ServiceHostAndPort>>(
                        new OkResponse<ServiceHostAndPort>(_services[i]));
                }
            }

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(_services[0]));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}