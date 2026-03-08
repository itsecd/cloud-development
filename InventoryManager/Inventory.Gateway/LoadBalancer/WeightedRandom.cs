using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Inventory.Gateway.LoadBalancer;

public class WeightedRandom(ILogger<WeightedRandom> logger, List<Service> services) : ILoadBalancer
{
    public string Type => "WeightedRandom";

    private readonly Random _rng = new();

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var totalWeight = 0;
        for (var i = 0; i < services.Count; i++)
            totalWeight += (i + 1) * (i + 1);
        
        var ticket = _rng.Next(totalWeight);

        var cumulative = 0;
        for (var i = 0; i < services.Count; i++)
        {
            var weight = (i + 1) * (i + 1);

            cumulative += weight;

            if (ticket <= cumulative)
            {
                var service = services[i];

                logger.LogInformation("WeightedRandom selected port {port}", service.HostAndPort.DownstreamPort);

                return Task.FromResult<Response<ServiceHostAndPort>>(
                    new OkResponse<ServiceHostAndPort>(service.HostAndPort));
            }
        }
    

        var fallback = services.Last();

        return Task.FromResult<Response<ServiceHostAndPort>>(
            new OkResponse<ServiceHostAndPort>(fallback.HostAndPort));
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}