using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace VehicleGen.Gateway.LoadBalancers;

public class WeightedRandomLoadBalancer : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _serviceProvider;
    private readonly int[] _weights;
    private readonly Random _random = new();

    public string Type => nameof(WeightedRandomLoadBalancer);

    public WeightedRandomLoadBalancer(Func<Task<List<Service>>> serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _weights = configuration.GetSection("LoadBalancer:Weights").Get<int[]>() ?? [];
    }

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var services = await _serviceProvider();

        if (services.Count == 0)
            throw new InvalidOperationException("Нет доступных сервисов для балансировки");

        var index = GetRandomIndexWithWeights(services.Count);
        return new OkResponse<ServiceHostAndPort>(services[index].HostAndPort);
    }

    private int GetRandomIndexWithWeights(int serviceCount)
    {
        if (_weights.Length == 0 || _weights.Length != serviceCount)
            return _random.Next(0, serviceCount);

        var totalWeight = _weights.Sum();
        var randomPoint = _random.NextDouble() * totalWeight;

        var current = 0.0;
        for (var i = 0; i < _weights.Length; i++)
        {
            var weight = _weights[i] > 0 ? _weights[i] : 1;
            current += weight;
            if (randomPoint <= current)
                return i;
        }

        return 0;
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}