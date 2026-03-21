using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace AspireApp.ApiGateway.LoadBalancing;

/// <summary>
/// Weighted Random балансировщик нагрузки.
/// Распределяет запросы между сервисами по заданным весам.
/// </summary>

public class WeightedRandomLoadBalancer : ILoadBalancingPolicy
using Ocelot.Errors;
using Ocelot.Responses;
using Ocelot.Values;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.Interfaces;

namespace AspireApp.ApiGateway.LoadBalancing;

public class WeightedRandomLoadBalancer : ILoadBalancer
{
    private readonly Random _random = new();

    public string Name => "WeightedRandom";

    public DestinationState? PickDestination(HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> destinations)
    public string Type => throw new NotImplementedException();


    public WeightedRandomLoadBalancer() 
    {
        _servicesProvider = null!;
        _weights = null!;
    }

    public WeightedRandomLoadBalancer(
        Func<Task<List<Service>>> servicesProvider,
        Dictionary<string, int> weights) : this()
    {
        if (destinations.Count == 0)
            return null;

        var weightedList = new List<DestinationState>();
        for (var i = 0; i < destinations.Count; i++)
        {
            var weight = i == 0 ? 5 : (i == 1 ? 3 : 2);
            for (var w = 0; w < weight; w++)
            {
                weightedList.Add(destinations[i]);
            }
        }

        var index = _random.Next(weightedList.Count);
        return weightedList[index];
    }
}

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _servicesProvider();
        if (services == null || services.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Нет доступных сервисов"));

        var available = services
            .Where(s => GetWeight(s) > 0)
            .ToList();

        if (available.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Нет сервисов с весом больше 0"));

        var weightedList = new List<Service>();
        foreach (var service in available)
        {
            var weight = GetWeight(service);
            for (var i = 0; i < weight; i++)
                weightedList.Add(service);
        }

        var selected = weightedList[_random.Next(weightedList.Count)];
        return new OkResponse<ServiceHostAndPort>(selected.HostAndPort);
    }

    private int GetWeight(Service service)
    {
        var key = $"{service.HostAndPort.DownstreamHost}:{service.HostAndPort.DownstreamPort}";
        return _weights?.TryGetValue(key, out var weight) == true ? weight : 1;
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}

public class ServicesAreEmptyError : Error
{
    public ServicesAreEmptyError(string message) 
        : base(message, OcelotErrorCode.ServicesAreEmptyError, 503) { }
}