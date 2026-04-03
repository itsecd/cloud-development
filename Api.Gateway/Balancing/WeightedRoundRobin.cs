using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.Balancing;

/// <summary>
/// Реализация алгоритма балансировки нагрузки "Взвешенный круговой обход" (Weighted Round Robin).
/// </summary>
/// <param name="services"></param>
/// <param name="configuration"></param>
public class WeightedRoundRobin(Func<Task<List<Service>>> services, IConfiguration configuration) : ILoadBalancer
{
    private static readonly object _locker = new();

    private readonly int[] _weights = configuration
        .GetSection("WeightsRoundRobin")
        .Get<int[]>() ?? [1, 2, 3, 4, 5];
    private int _lastIndex = 0;
    private int _currentWeight = 0;

    public string Type => nameof(WeightedRoundRobin);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var servicesList = await services();
        if (servicesList.Count == 0)
            throw new InvalidOperationException("No available downstream services");
        lock (_locker)
        {
            if (_currentWeight >= _weights[_lastIndex])
            {
                _currentWeight = 0;
                _lastIndex = (_lastIndex + 1 >= _weights.Length) ? 0 : _lastIndex + 1;
            }
            _currentWeight++;
            return new OkResponse<ServiceHostAndPort>(servicesList[_lastIndex].HostAndPort);
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}