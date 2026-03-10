using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CompanyEmployee.ApiGateway;

public class QueryBasedLoadBalancer(Func<Task<List<Service>>> services, ILogger<QueryBasedLoadBalancer> logger) 
    : ILoadBalancer
{
    
    public string Type => nameof(QueryBasedLoadBalancer);
    
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var availableServices = await services.Invoke();
        if (availableServices.Count == 0)
        {
            logger.LogError("No services available");
            return new ErrorResponse<ServiceHostAndPort>(
                new UnableToFindDownstreamRouteError(context.Request.Path.Value, context.Request.Method));
        }
        
        var idStr = context.Request.Query["id"].FirstOrDefault("error");
        if (!int.TryParse(idStr, out var index))
        {
            logger.LogWarning($"Could not parse id: {idStr}, random replica will be selected", idStr);
            index = Random.Shared.Next(availableServices.Count);
        }
        
        index %= availableServices.Count;
        if (index < 0)
        {
            index += availableServices.Count;
        }
        
        logger.LogInformation($"Using replica with index: {index}", index);
        
        return new OkResponse<ServiceHostAndPort>(availableServices[index].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}