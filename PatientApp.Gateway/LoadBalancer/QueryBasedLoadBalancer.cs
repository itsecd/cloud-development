using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;
using Ocelot.ServiceDiscovery.Providers;

namespace PatientApp.Gateway.LoadBalancer;

public class QueryBasedLoadBalancer : ILoadBalancer
{
    private readonly IServiceDiscoveryProvider _serviceDiscovery;

    public QueryBasedLoadBalancer(IServiceDiscoveryProvider serviceDiscovery)
    {
        _serviceDiscovery = serviceDiscovery;
    }

    public string Type => "QueryBasedLoadBalancer";

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _serviceDiscovery.GetAsync();

        if (services == null || services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                    new ServicesAreNullError("No services available")
                );
        }

        var idStr = httpContext.Request.Query["id"].FirstOrDefault();

        if (!int.TryParse(idStr, out var id))
        {
            return new OkResponse<ServiceHostAndPort>(services[0].HostAndPort);
        }

        var selected = services[id % services.Count];

        return new OkResponse<ServiceHostAndPort>(selected.HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}