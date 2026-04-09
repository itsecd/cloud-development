using Ocelot.Configuration;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace ProjectApp.Gateway.LoadBalancing;

public class QueryBasedLoadBalancerCreator : ILoadBalancerCreator
{
    public string Type => "QueryBased";

    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        var loadBalancer = new QueryBasedLoadBalancer(serviceProvider);
        return new OkResponse<ILoadBalancer>(loadBalancer);
    }
}
