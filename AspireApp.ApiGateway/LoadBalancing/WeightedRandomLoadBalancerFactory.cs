using Ocelot.Configuration;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace AspireApp.ApiGateway.LoadBalancing;

/// <summary>
/// Фабрика для создания WeightedRandomLoadBalancer.
/// </summary>
public class WeightedRandomLoadBalancerFactory : ILoadBalancerFactory
{
    public Response<ILoadBalancer> Get(DownstreamRoute route, ServiceProviderConfiguration serviceProviderConfiguration)
    {
        var services = route.DownstreamAddresses
            .Select(x => new Service("service", new ServiceHostAndPort(x.Host, x.Port), "", "", new List<string>()))
            .ToList();

        return new OkResponse<ILoadBalancer>(new WeightedRandomLoadBalancer(services));
    }
}