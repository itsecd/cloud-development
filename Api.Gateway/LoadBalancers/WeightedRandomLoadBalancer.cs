using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;
using System.Collections.Generic;

namespace Api.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки на основе взвешенного случайного выбора (Weighted Random).
/// Каждой реплике назначается вероятность выбора. При поступлении запроса
/// реплика выбирается случайно с учётом заданных вероятностей.
/// Веса задаются в appsettings.json в секции "WeightedRandomWeights".
/// </summary>
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

    private static double[] BuildCumulativeWeights(double[] weights)
    {
        var cumulative = new double[weights.Length];
        cumulative[0] = weights[0];
        for (var i = 1; i < weights.Length; i++)
            cumulative[i] = cumulative[i - 1] + weights[i];
        return cumulative;
    }
}
