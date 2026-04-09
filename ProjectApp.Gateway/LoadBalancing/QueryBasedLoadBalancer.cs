using Ocelot.LoadBalancer;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace ProjectApp.Gateway.LoadBalancing;

public class QueryBasedLoadBalancer(IServiceDiscoveryProvider serviceProvider) : ILoadBalancer
{
    public string Type => "QueryBased";

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await serviceProvider.GetAsync();
        if (services is null)
        {
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError("No downstream services available."));
        }

        if (services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("No downstream services available."));
        }

        var orderedServices = services
            .OrderBy(s => s.HostAndPort.DownstreamHost, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.HostAndPort.DownstreamPort)
            .ToArray();

        var id = ExtractRequestId(httpContext);
        var replicaIndex = (int)(((long)id % orderedServices.Length + orderedServices.Length) % orderedServices.Length);
        var selectedService = orderedServices[replicaIndex];

        return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
        // No-op: the algorithm is stateless and selection is fully deterministic by query parameter.
    }

    private static int ExtractRequestId(HttpContext httpContext)
    {
        var rawId = httpContext.Request.Query["id"].ToString();
        return int.TryParse(rawId, out var parsedId) ? parsedId : 0;
    }
}
