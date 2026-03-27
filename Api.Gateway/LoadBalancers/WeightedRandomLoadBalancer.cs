using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки на основе взвешенного случайного выбора (Weighted Random).
/// Каждой реплике назначается вероятность выбора. При поступлении запроса
/// реплика выбирается случайно с учётом заданных вероятностей.
/// Веса задаются в appsettings.json в секции "WeightedRandomWeights".
/// </summary>
/// <param name="services">Фабрика для получения списка доступных сервисов.</param>
/// <param name="configuration">Конфигурация приложения для чтения весов из секции "WeightedRandomWeights".</param>
public class WeightedRandomLoadBalancer(Func<Task<List<Service>>> services, IConfiguration configuration) : ILoadBalancer
{
    private readonly double[] _cumulativeWeights = BuildCumulativeWeights(
        configuration.GetSection("WeightedRandomWeights").Get<double[]>() ?? [0.4, 0.3, 0.15, 0.1, 0.05]);

    public string Type => nameof(WeightedRandomLoadBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var availableServices = await services();

        if (availableServices.Count == 0)
            throw new InvalidOperationException("No available downstream services");

        var index = Array.BinarySearch(_cumulativeWeights, Random.Shared.NextDouble());
        if (index < 0) index = ~index;

        return new OkResponse<ServiceHostAndPort>(
            availableServices[Math.Min(index, availableServices.Count - 1)].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }

    /// <summary>
    /// Строит массив кумулятивных весов на основе входных весов.
    /// Каждый элемент результирующего массива равен сумме всех предыдущих весов включительно.
    /// Используется для выбора реплики методом бинарного поиска по случайному числу.
    /// </summary>
    /// <param name="weights">Массив весов для каждой реплики.</param>
    /// <returns>Массив кумулятивных весов.</returns>
    private static double[] BuildCumulativeWeights(double[] weights)
    {
        var total = weights.Sum();
        var cumulative = new double[weights.Length];
        cumulative[0] = weights[0] / total;
        for (var i = 1; i < weights.Length; i++)
            cumulative[i] = cumulative[i - 1] + weights[i] / total;
        return cumulative;
    }
}
