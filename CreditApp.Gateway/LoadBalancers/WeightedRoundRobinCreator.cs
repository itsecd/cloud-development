using Ocelot.Configuration;
using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace CreditApp.Gateway.LoadBalancers;

public class WeightedRoundRobinCreator : ILoadBalancerCreator
{
    public string Type => "WeightedRoundRobin";

    public Response<ILoadBalancer> Create(
        DownstreamRoute route,
        IServiceDiscoveryProvider serviceDiscoveryProvider)
    {
        var services = route.DownstreamAddresses
            .Select(x => new ServiceHostAndPort(x.Host, x.Port))
            .ToList();

        var balancer = new WeightedRoundRobinLoadBalancer(services);

        return new OkResponse<ILoadBalancer>(balancer);
    }
}