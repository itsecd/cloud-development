using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Inventory.Gateway.LoadBalancer;

public class RandomSelector(ILogger<RandomSelector> logger, List<Service> instances) : ILoadBalancer
{
    public string Type => "RandomSelector";

    private static int ParseRequestId(HttpContext context)
    {
        var raw = context.Request.Query["id"].FirstOrDefault();

        return int.TryParse(raw, out var value) ? value : -1;
    }

    private Service SelectInstance()
    {
        var index = Random.Shared.Next(instances.Count);

        return instances[index];
    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        if (instances.Count == 0)
        {
            return Task.FromResult<Response<ServiceHostAndPort>>(
                new ErrorResponse<ServiceHostAndPort>(
                    new UnableToFindDownstreamRouteError(
                        context.Request.Path,
                        context.Request.Method
                    )
                )
            );
        }

        var requestId = ParseRequestId(context);

        var selected = SelectInstance();

        logger.LogInformation(
            "Gateway selected instance {Host}:{Port} for request id {Id}",
            selected.HostAndPort.DownstreamHost,
            selected.HostAndPort.DownstreamPort,
            requestId
        );

        return Task.FromResult<Response<ServiceHostAndPort>>(
            new OkResponse<ServiceHostAndPort>(selected.HostAndPort)
        );
    }

    public void Release(ServiceHostAndPort hostAndPort){ }
}