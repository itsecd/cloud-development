using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway;

/// <summary>
/// Балансировщик нагрузки «Взвешенная карусель» (Weighted Round Robin).
/// Реплики перебираются циклически; каждая обрабатывает ровно <c>W</c> запросов подряд,
/// где <c>W</c> — её вес из секции <c>LoadBalancer:Weights</c> в конфигурации (0-based).
/// </summary>
/// <param name="services">Делегат для получения списка доступных реплик сервиса.</param>
/// <param name="configuration">Конфигурация приложения с секцией <c>LoadBalancer:Weights</c>.</param>
public class WeightedRoundRobin(
    Func<Task<List<Service>>> services,
    IConfiguration configuration) : ILoadBalancer
{
    public string Type => nameof(WeightedRoundRobin);

    private readonly int[] _weights = configuration.GetSection("LoadBalancer:Weights").Get<int[]>() ?? [];
    private readonly object _lock = new();
    private int _index;
    private int _used;

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var pool = await services();

        if (pool.Count == 0)
            throw new InvalidOperationException("No available downstream services");

        lock (_lock)
        {
            if (_index >= pool.Count)
            {
                _index = 0;
                _used = 0;
            }

            if (_used >= Weight(_index))
            {
                _index = (_index + 1) % pool.Count;
                _used = 0;
            }

            _used++;
            return new OkResponse<ServiceHostAndPort>(pool[_index].HostAndPort);
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }

    /// <summary>Возвращает вес реплики по индексу. Если не задан или не положителен — возвращает 1.</summary>
    /// <param name="i">Индекс реплики.</param>
    private int Weight(int i) => i < _weights.Length && _weights[i] > 0 ? _weights[i] : 1;
}
