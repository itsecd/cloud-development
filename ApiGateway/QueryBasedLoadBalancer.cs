using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway;

public class QueryBasedLoadBalancer : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _services;

    public QueryBasedLoadBalancer()
    {
        _services = () => Task.FromResult(new List<Service>());
    }

    public QueryBasedLoadBalancer(Func<Task<List<Service>>> services)
    {
        _services = services;
    }

    public string Type => nameof(QueryBasedLoadBalancer);

    public void Release(ServiceHostAndPort hostAndPort) { }

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _services();

        if (services == null || services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new NoServicesAvailableError("No services available for load balancing"));
        }

        if (!httpContext.Request.Query.TryGetValue("id", out var idValues)
            || !int.TryParse(idValues.FirstOrDefault(), out var id))
        {
            var firstService = services[0];
            return new OkResponse<ServiceHostAndPort>(firstService.HostAndPort);
        }

        var replicaIndex = id % services.Count;
        var selectedService = services[replicaIndex];

        return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
    }
}
