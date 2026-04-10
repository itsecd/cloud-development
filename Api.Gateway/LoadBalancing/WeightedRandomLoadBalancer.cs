using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancing;

/// <summary>
/// Балансировка случайным образом с весами
/// </summary>
/// <param name="services">Функция получения списка сервисов</param>
/// <param name="configuration">Конфигурация приложения</param>
public class WeightedRandomLoadBalancer(
    Func<Task<List<Service>>> services,
    IConfiguration configuration) : ILoadBalancer
{
    private readonly int[] _frequencies = configuration.GetSection("LoadBalancing:Weights").Get<int[]>() ?? [5, 4, 3, 2, 1];

    public string Type => nameof(WeightedRandomLoadBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var availableServices = await services();

        if (availableServices.Count == 0)
            throw new InvalidOperationException("No available downstream services");

        var values = Enumerable.Range(1, availableServices.Count)
            .Zip(_frequencies, (val, freq) => Enumerable.Repeat(val, freq))
            .SelectMany(x => x)
            .ToArray();

        Random.Shared.Shuffle(values);

        return new OkResponse<ServiceHostAndPort>(availableServices[values.First() - 1].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
