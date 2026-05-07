using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway;

/// <summary>
/// Балансировщик нагрузки Weighted Round Robin (взвешенная карусель).
/// Каждой реплике сервиса присваивается вес, в зависимости от которого она используется чаще/реже других.
/// Реплики перебираются циклически: для весов [3, 2, 1] последовательность будет R1, R1, R1, R2, R2, R3, R1, R1, R1, ...
/// </summary>
/// <param name="services">Делегат получения актуального списка downstream-реплик от Ocelot.</param>
/// <param name="configuration">
/// Конфигурация приложения. Веса считываются из секции <c>LoadBalancer:Weights</c> как массив <see cref="int"/>
/// в порядке реплик. Если для реплики вес отсутствует или некорректен (≤ 0), используется значение 1.
/// </param>
public class WeightedRoundRobinLoadBalancer(
    Func<Task<List<Service>>> services,
    IConfiguration configuration) : ILoadBalancer
{
    private readonly int[] _weights = configuration
        .GetSection("LoadBalancer:Weights").Get<int[]>() ?? [];

    private long _counter = -1;

    public string Type => nameof(WeightedRoundRobinLoadBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var list = await services();

        if (list.Count == 0)
            throw new InvalidOperationException("No available downstream services.");

        var weights = NormalizeWeights(list.Count);
        var total = weights.Sum();

        var pick = (int)(Interlocked.Increment(ref _counter) % total);
        if (pick < 0) pick += total;

        for (var i = 0; i < list.Count; i++)
        {
            pick -= weights[i];
            if (pick < 0)
                return new OkResponse<ServiceHostAndPort>(list[i].HostAndPort);
        }

        return new OkResponse<ServiceHostAndPort>(list[^1].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }

    private int[] NormalizeWeights(int count)
    {
        var result = new int[count];
        for (var i = 0; i < count; i++)
            result[i] = i < _weights.Length && _weights[i] > 0 ? _weights[i] : 1;
        return result;
    }
}
