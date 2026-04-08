using Ocelot.Responses;
using Ocelot.Values;
using Ocelot.LoadBalancer.Interfaces;

namespace AspireApp.ApiGateway.LoadBalancing;

/// <summary>
/// Weighted Random балансировщик нагрузки.
/// Распределяет запросы между сервисами по заданным весам.
/// </summary>
public class WeightedRandomLoadBalancer : ILoadBalancer
{
    private readonly List<Service> _services;
    private readonly List<int> _weights;
    private readonly int _totalWeight;
    private static readonly Random _random = Random.Shared;
    private readonly IConfiguration _configuration;

    public WeightedRandomLoadBalancer(List<Service> services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
        _weights = services.Select(s => GetWeight(s)).ToList();
        _totalWeight = _weights.Sum();
    }

    private int GetWeight(Service service)
    {
        var port = service.HostAndPort.DownstreamPort.ToString();
        return _configuration.GetValue<int>($"Weights:{port}", 1);
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
    public string Type => "WeightedRandom";
}
