using Ocelot.Configuration;
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
        IServiceDiscoveryProvider serviceProvider)
    {
        var services = serviceProvider.GetAsync().Result;

        var hostAndPorts = services
            .Select(s => s.HostAndPort)
            .ToList();

        var balancer = new WeightedRoundRobinLoadBalancer(hostAndPorts);

        return new OkResponse<ILoadBalancer>(balancer);
    }
}