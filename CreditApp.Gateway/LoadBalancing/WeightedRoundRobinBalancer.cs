using Ocelot.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace CreditApp.Gateway.LoadBalancing;

/// <summary>
/// Кастомный балансировщик нагрузки, реализующий алгоритм Weighted Round Robin.
/// Каждый downstream-хост получает количество запросов, пропорциональное его весу.
/// Веса привязаны к именам сервисов через маппинг hostPort → serviceName.
/// </summary>
public class WeightedRoundRobinBalancer(
    IServiceDiscoveryProvider serviceDiscoveryProvider,
    Dictionary<string, int> replicaWeights,
    Dictionary<string, string> hostPortToServiceName) : ILoadBalancer
{
    private readonly List<ServiceHostAndPort> _weightedServices =
        BuildWeightedList(serviceDiscoveryProvider, replicaWeights, hostPortToServiceName);

    private int _currentIndex = -1;
    private readonly object _lock = new();

    public string Type => nameof(WeightedRoundRobinBalancer);

    /// <summary>
    /// Строит развёрнутый список хостов: хост с весом 3 появляется 3 раза.
    /// </summary>
    private static List<ServiceHostAndPort> BuildWeightedList(
        IServiceDiscoveryProvider provider,
        Dictionary<string, int> replicaWeights,
        Dictionary<string, string> hostPortToServiceName)
    {
        var services = provider.GetAsync().Result;
        var list = new List<ServiceHostAndPort>();

        foreach (var service in services)
        {
            var key = $"{service.HostAndPort.DownstreamHost}:{service.HostAndPort.DownstreamPort}";
            var weight = 1;

            if (hostPortToServiceName.TryGetValue(key, out var serviceName)
                && replicaWeights.TryGetValue(serviceName, out var configuredWeight))
            {
                weight = configuredWeight;
            }

            for (var i = 0; i < weight; i++)
                list.Add(service.HostAndPort);
        }

        return list;
    }

    /// <summary>
    /// Выбирает следующий хост по алгоритму Weighted Round Robin.
    /// </summary>
    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        lock (_lock)
        {
            if (_weightedServices.Count == 0)
            {
                return Task.FromResult<Response<ServiceHostAndPort>>(
                    new ErrorResponse<ServiceHostAndPort>(new List<Error>()));
            }

            _currentIndex = (_currentIndex + 1) % _weightedServices.Count;
            var selected = _weightedServices[_currentIndex];

            var key = $"{selected.DownstreamHost}:{selected.DownstreamPort}";
            var serviceName = hostPortToServiceName.GetValueOrDefault(key, key);

            httpContext.Items["SelectedService"] = serviceName;

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(selected));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
