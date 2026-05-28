using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace VehicleGen.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки с взвешенным случайным выбором
/// </summary>
public class WeightedRandomLoadBalancer : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _serviceFetcher;
    private readonly int[] _weights;

    public string Type => nameof(WeightedRandomLoadBalancer);

    /// <summary>
    /// Создаёт балансировщик с указанными весами
    /// </summary>
    /// <param name="serviceFetcher">Ввозвращает список доступных сервисов</param>
    /// <param name="configuration">Конфигурация приложения</param>
    public WeightedRandomLoadBalancer(Func<Task<List<Service>>> serviceFetcher, IConfiguration configuration)
    {
        _serviceFetcher = serviceFetcher;
        _weights = configuration.GetSection("LoadBalancer:Weights").Get<int[]>() ?? [];
    }

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var services = await _serviceFetcher();

        if (services.Count == 0)
            throw new InvalidOperationException("No available services for load balancing");

        var index = GetRandomIndexWithWeights(services.Count);
        return new OkResponse<ServiceHostAndPort>(services[index].HostAndPort);
    }

    /// <summary>
    /// Получает случайный индекс с учётом весов
    /// </summary>
    private int GetRandomIndexWithWeights(int serviceCount)
    {
        if (_weights.Length == 0 || _weights.Length != serviceCount)
            return Random.Shared.Next(0, serviceCount);

        var totalWeight = _weights.Sum();
        var randomPoint = Random.Shared.NextDouble() * totalWeight;

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