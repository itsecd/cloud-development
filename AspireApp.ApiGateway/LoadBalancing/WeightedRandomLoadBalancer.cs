using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.LoadBalancer.Abstractions;
using Ocelot.Responses;
using Ocelot.Values;

namespace AspireApp.ApiGateway.LoadBalancing;

/// <summary>
/// Weighted Random балансировщик
/// </summary>
public class WeightedRandomLoadBalancer : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _servicesProvider;
    private readonly Dictionary<string, int> _weights;
    private readonly Random _random = new();

    public WeightedRandomLoadBalancer(
        Func<Task<List<Service>>> servicesProvider,
        Dictionary<string, int> weights)
    {
        _servicesProvider = servicesProvider;
        _weights = weights;
    }

    public string Type => "WeightedRandom";

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _servicesProvider();
        if (services?.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Нет сервисов"));

        var available = services
            .Where(s => _weights.GetValueOrDefault($"{s.HostAndPort.DownstreamHost}:{s.HostAndPort.DownstreamPort}", 1) > 0)
            .ToList();

        if (available.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Нет сервисов с весом больше 0"));

        var weighted = available.SelectMany(s => 
            Enumerable.Repeat(s, _weights.GetValueOrDefault($"{s.HostAndPort.DownstreamHost}:{s.HostAndPort.DownstreamPort}", 1))).ToList();

        return new OkResponse<ServiceHostAndPort>(weighted[_random.Next(weighted.Count)].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}

public class ServicesAreEmptyError : Error
{
    public ServicesAreEmptyError(string message) : base(message, OcelotErrorCode.ServicesAreEmptyError, 503) { }
}