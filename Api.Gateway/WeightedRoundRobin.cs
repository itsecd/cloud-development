using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway;

/// <summary>
/// Балансировщик Weighted Round Robin: распределяет запросы циклически
/// с учётом весов реплик из секции <c>WeightedRoundRobin:Weights</c>.
/// </summary>
/// <param name="serviceProviderFactory">Делегат, возвращающий список реплик.</param>
/// <param name="configuration">Источник весов.</param>
public class WeightedRoundRobin(Func<Task<List<Service>>> serviceProviderFactory, IConfiguration configuration) : ILoadBalancer
{
    private readonly int[] _weights = configuration.GetSection("WeightedRoundRobin:Weights").Get<int[]>() ?? [];
    private long _counter = -1;

    public string Type => nameof(WeightedRoundRobin);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var services = await serviceProviderFactory();

        if (services.Count == 0)
            throw new InvalidOperationException("No available downstream services");

        var serviceIndex = SelectIndex(services.Count);

        return new OkResponse<ServiceHostAndPort>(services[serviceIndex].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }

    private int SelectIndex(int serviceCount)
    {
        var totalWeight = 0L;
        for (var i = 0; i < serviceCount; i++)
            totalWeight += GetWeight(i);

        var ticket = (Interlocked.Increment(ref _counter) & long.MaxValue) % totalWeight;

        var cumulative = 0L;
        for (var i = 0; i < serviceCount; i++)
        {
            cumulative += GetWeight(i);
            if (ticket < cumulative)
                return i;
        }

        return serviceCount - 1;
    }

    private int GetWeight(int index)
    {
        if (index >= _weights.Length) return 1;
        var weight = _weights[index];
        return weight > 0 ? weight : 1;
    }
}
