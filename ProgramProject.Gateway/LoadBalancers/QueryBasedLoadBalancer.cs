using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ProgramProject.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик для алгоритма Query Based
/// </summary>
public class QueryBasedLoadBalancer : ILoadBalancer
{
    private readonly List<Service> _services;
    private readonly ILogger<QueryBasedLoadBalancer> _logger;
    private readonly string _queryParameterName;

    public QueryBasedLoadBalancer(List<Service> services, ILogger<QueryBasedLoadBalancer> logger, string queryParameterName = "id")
    {
        _services = services;
        _logger = logger;
        _queryParameterName = queryParameterName;
    }

    public string Type => nameof(QueryBasedLoadBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var idValue = ExtractIdFromQuery(httpContext);
        var replicaIndex = Math.Abs(idValue) % _services.Count;
        var selectedService = _services[replicaIndex];

        _logger.LogInformation("Запрос с id={Id} направлен на реплику {Index}", idValue, replicaIndex);

        return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
    }

    private int ExtractIdFromQuery(HttpContext context)
    {
        if (context.Request.Query.TryGetValue(_queryParameterName, out var idString))
        {
            if (int.TryParse(idString, out var id))
                return id;
        }
        return 0;
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}