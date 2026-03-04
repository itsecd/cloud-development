using Ocelot.LoadBalancer.Errors;
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
    private readonly (List<(ServiceHostAndPort Host, int Weight)> Services, int TotalWeight) _config =
        BuildWeightedList(serviceDiscoveryProvider, replicaWeights, hostPortToServiceName);

    private int _currentIndex = -1;
    private readonly object _lock = new();

    public string Type => nameof(WeightedRoundRobinBalancer);

    /// <summary>
    /// Строит развёрнутый список хостов: хост с весом 3 появляется 3 раза.
    /// </summary>
    private static (List<(ServiceHostAndPort Host, int Weight)> Services, int TotalWeight) BuildWeightedList(
        IServiceDiscoveryProvider provider,
        Dictionary<string, int> replicaWeights,
        Dictionary<string, string> hostPortToServiceName)
    {
        var services = provider.GetAsync().Result;
        var list = new List<(ServiceHostAndPort Host, int Weight)>();
        var totalWeight = 0;

        foreach (var service in services)
        {
            var key = $"{service.HostAndPort.DownstreamHost}:{service.HostAndPort.DownstreamPort}";
            var weight = 1;

            if (hostPortToServiceName.TryGetValue(key, out var serviceName)
                && replicaWeights.TryGetValue(serviceName, out var configuredWeight))
            {
                weight = configuredWeight;
            }

            list.Add((service.HostAndPort, weight));
            totalWeight += weight;
        }

        return (list, totalWeight);
    }

    /// <summary>
    /// Выбирает следующий хост по алгоритму Weighted Round Robin.
    /// </summary>
    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        if (_config.Services.Count == 0)
        {
            return Task.FromResult<Response<ServiceHostAndPort>>(
                new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Generator services list is empty, no hosts were discovered")));
        }

        lock (_lock)
        {
            _currentIndex = (_currentIndex + 1) % _config.TotalWeight;

            var cumulative = 0;

            foreach (var (host, weight) in _config.Services)
            {
                cumulative += weight;
                if (_currentIndex < cumulative)
                {
                    var key = $"{host.DownstreamHost}:{host.DownstreamPort}";
                    var serviceName = hostPortToServiceName.GetValueOrDefault(key, key);
                    httpContext.Items["SelectedService"] = serviceName;

                    return Task.FromResult<Response<ServiceHostAndPort>>(
                        new OkResponse<ServiceHostAndPort>(host));
                }
            }

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Could not match any service host within cumulative weight bounds")));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
