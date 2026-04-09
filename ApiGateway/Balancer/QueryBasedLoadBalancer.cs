using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace ApiGateway.Balancer;

public sealed class QueryBasedLoadBalancer(
    IServiceDiscoveryProvider serviceDiscovery) : ILoadBalancer
{
    public string Type => nameof(QueryBasedLoadBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await serviceDiscovery.GetAsync();

        if (services is null)
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreNullError("Service discovery returned null"));

        if (services.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreNullError("No downstream services are available"));

        var service = SelectByQuery(httpContext, services);
        return new OkResponse<ServiceHostAndPort>(service.HostAndPort);
    }

    private static Service SelectByQuery(HttpContext httpContext, List<Service> services)
    {
        var idRaw = httpContext.Request.Query["id"].FirstOrDefault();

        if (!int.TryParse(idRaw, out var id))
            return services[0];

        var index = Math.Abs(id % services.Count);
        return services[index];
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}