using Ocelot.Configuration;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CompanyEmployee.Gateway.LoadBalancers;

/// <summary>
/// Фабрика для создания взвешенного случайного балансировщика.
/// </summary>
public class WeightedRandomLoadBalancerFactory : ILoadBalancerFactory
{
    private readonly Dictionary<string, int> _defaultWeights = new()
    {
        { "localhost:6001", 5 },
        { "localhost:6002", 4 },
        { "localhost:6003", 3 },
        { "localhost:6004", 2 },
        { "localhost:6005", 1 }
    };

    /// <inheritdoc />
    public Response<ILoadBalancer> Get(DownstreamRoute route, ServiceProviderConfiguration config)
    {
        var services = new List<Service>();

        foreach (var address in route.DownstreamAddresses)
        {
            var serviceHostAndPort = new ServiceHostAndPort(
                address.Host,
                address.Port,
                route.DownstreamScheme);

            var service = new Service(
                $"{address.Host}:{address.Port}",
                serviceHostAndPort,
                string.Empty,
                string.Empty,
                Enumerable.Empty<string>());

            services.Add(service);
        }

        var loadBalancer = new WeightedRandomLoadBalancer(services, _defaultWeights);
        return new OkResponse<ILoadBalancer>(loadBalancer);
    }
}