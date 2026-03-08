using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Responses;
using Ocelot.Values;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.Interfaces;

namespace AspireApp.ApiGateway.LoadBalancing;

public class WeightedRandomLoadBalancer : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _servicesProvider;
    private readonly Dictionary<string, int> _weights;
    private readonly Random _random = new();

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
        _servicesProvider = servicesProvider;
        _weights = weights;
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
