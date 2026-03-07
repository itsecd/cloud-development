using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CourseManagement.ApiGateway.LoadBalancers;

public class QueryLoadBalancer(ILogger<QueryLoadBalancer> logger, List<Service> services) : ILoadBalancer
{
    private readonly List<Service> _services = services;

    private readonly Lock _lock = new();

    public string Type => "QueryLoadBalancer";

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        lock (_lock)
        {
            var idString = httpContext.Request.Query["id"].FirstOrDefault();

            if (!int.TryParse(idString, out var id))
                id = 0;

            var service = _services[id % _services.Count];

            logger.LogInformation("Request {ResourceId} sent to service on port {servicePort}", id, service.HostAndPort.DownstreamPort);

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(service.HostAndPort));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
