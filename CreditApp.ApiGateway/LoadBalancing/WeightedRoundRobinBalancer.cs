using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace CreditApp.ApiGateway.LoadBalancing;

/// <summary>
/// Weighted Round Robin балансировщик нагрузки для Ocelot
/// </summary>
public class WeightedRoundRobinLoadBalancer(Func<Task<List<Service>>> servicesProvider, Dictionary<string, double> hostPortWeights) : ILoadBalancer
{
    private int _currentIndex = -1;
    private int _currentWeight = 0;
    private readonly object _lock = new();

    public async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
    {
        var services = await servicesProvider();

        if (services == null || services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreEmptyError("No services available"));
        }

        lock (_lock)
        {
            var maxWeight = hostPortWeights.Values
                .Select(w => (int)Math.Ceiling(w))
                .DefaultIfEmpty(1)
                .Max();

            while (true)
            {
                _currentIndex = (_currentIndex + 1) % services.Count;

                if (_currentIndex == 0)
                {
                    _currentWeight--;
                    if (_currentWeight <= 0)
                    {
                        _currentWeight = maxWeight;
                    }
                }

                var service = services[_currentIndex];
                var hostPort = $"{service.HostAndPort.DownstreamHost}:{service.HostAndPort.DownstreamPort}";

                var weight = hostPortWeights.TryGetValue(hostPort, out var w) ? w : 1.0;

                if ((int)Math.Ceiling(weight) >= _currentWeight)
                {
                    return new OkResponse<ServiceHostAndPort>(service.HostAndPort);
                }
            }
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}
