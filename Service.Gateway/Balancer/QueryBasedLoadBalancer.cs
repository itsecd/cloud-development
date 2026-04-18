using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Values;
using Ocelot.Responses;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Errors;

namespace Service.Gateway.Balancer;
/// <summary>
/// Ocelot load balancer that selects a downstream service based on the value of id parametr in query.
/// </summary>
public class QueryBasedLoadBalancer(Func<Task<List<Ocelot.Values.Service>>> services) : ILoadBalancer
{
    private readonly Func<Task<List<Ocelot.Values.Service>>> _services = services;

    public string Type => nameof(QueryBasedLoadBalancer);
    /// <summary>
    /// get downstream service selected for the request
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _services();
        if (services == null || services.Count == 0) {
            throw new InvalidOperationException("QueryBasedLoadBalancer couldnt select an downstream services cuz no downstream services were available");
        }
        var strId = httpContext.Request.Query["id"].ToString();
        int id;
        if (!int.TryParse(strId, out id))
        {
            return new OkResponse<ServiceHostAndPort>(services[0].HostAndPort);
        }
        var index = Math.Abs(id % services.Count);
        return new OkResponse<ServiceHostAndPort>(services[index].HostAndPort);
    }
    /// <summary>
    /// releases Lease instance
    /// </summary>
    /// <param name="hostAndPort"></param>
    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}
