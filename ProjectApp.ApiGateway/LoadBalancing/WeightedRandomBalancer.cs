using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ProjectApp.ApiGateway.LoadBalancing;

/// <summary>
/// Weighted Random балансировщик нагрузки для Ocelot.
/// </summary>
public class WeightedRandomBalancer(
    Func<Task<List<Service>>> servicesProvider,
    Dictionary<string, int> hostPortWeights) : ILoadBalancer
{
    public string Type => nameof(WeightedRandomBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await servicesProvider();

        if (services == null || services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreEmptyError("No services available"));
        }

        var weightedServices = services
            .Select(service => new
            {
                Service = service,
                Weight = hostPortWeights.GetValueOrDefault(
                    $"{service.HostAndPort.DownstreamHost}:{service.HostAndPort.DownstreamPort}",
                    1)
            })
            .Where(item => item.Weight > 0)
            .ToList();

        if (weightedServices.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreEmptyError("No services with positive weight available"));
        }

        var totalWeight = weightedServices.Sum(item => item.Weight);
        var roll = Random.Shared.Next(totalWeight);
        var currentWeight = 0;

        foreach (var item in weightedServices)
        {
            currentWeight += item.Weight;
            if (roll < currentWeight)
            {
                return new OkResponse<ServiceHostAndPort>(item.Service.HostAndPort);
            }
        }

        return new OkResponse<ServiceHostAndPort>(weightedServices[^1].Service.HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}
