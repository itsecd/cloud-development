using Ocelot.Configuration;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace PatientApp.Gateway.LoadBalancer;
public class QueryBasedLoadBalancerCreator : ILoadBalancerCreator
{
    public string Type => "QueryBased";

    public Response<ILoadBalancer> Create(
        DownstreamRoute downstreamRoute,
        IServiceDiscoveryProvider serviceDiscoveryProvider)
    {
        var lb = new QueryBasedLoadBalancer(serviceDiscoveryProvider);
        return new OkResponse<ILoadBalancer>(lb);
    }
}