using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.LoadBalancer.Errors;

namespace AspireApp.ApiGateway.LoadBalancing;

/// <summary>
/// Weighted Random балансировщик нагрузки.
/// Распределяет запросы между сервисами по заданным весам.
/// </summary>
public class WeightedRandomLoadBalancer(List<Service> services) : ILoadBalancer
{
    private readonly List<Service> _services = services;
    private readonly List<int> _weights = services.Select(GetWeight).ToList();
    private readonly int _totalWeight = services.Sum(GetWeight);
    private static readonly Random _random = Random.Shared;

    private static int GetWeight(Service service)
    {
        var port = service.HostAndPort.DownstreamPort;
        return port == 5001 ? 5 : port == 5002 ? 3 : port == 5003 ? 2 : 1;
    }

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        if (_services.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Нет доступных сервисов"));

        var randomValue = _random.Next(_totalWeight);

        var current = 0;
        for (var i = 0; i < _services.Count; i++)
        {
            current += _weights[i];
            if (randomValue < current)
            {
                return new OkResponse<ServiceHostAndPort>(_services[i].HostAndPort);
            }
        }

        return new OkResponse<ServiceHostAndPort>(_services[0].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }

    public string Name => nameof(WeightedRandomLoadBalancer);
    public string Type => "WeightedRandom";
}