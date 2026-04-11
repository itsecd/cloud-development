using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;
using Ocelot.ServiceDiscovery.Providers;

namespace PatientApp.Gateway.LoadBalancer;

public class QueryBasedLoadBalancer : ILoadBalancer
{
    private readonly IServiceDiscoveryProvider _serviceDiscoveryProvider;

    public QueryBasedLoadBalancer(IServiceDiscoveryProvider serviceDiscoveryProvider)
    {
        _serviceDiscoveryProvider = serviceDiscoveryProvider;
    }

    public string Type => "QueryBased";

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _serviceDiscoveryProvider.GetAsync();

        if (services == null || services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new UnableToFindLoadBalancerError("No services available")
            );
        }

        var idStr = httpContext.Request.Query["id"].FirstOrDefault();

        if (!int.TryParse(idStr, out var id))
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new UnableToFindLoadBalancerError("Invalid or missing id")
            );
        }

        var index = id % services.Count;

        var selected = services[index];

        var result = selected.HostAndPort;

        return new OkResponse<ServiceHostAndPort>(result);
    }

    public void Release(ServiceHostAndPort service)
    {
    }
}