using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using ProjectApp.Gateway.Configuration;

namespace ProjectApp.Gateway.LoadBalancer;

/// <summary>
/// Создатель балансировщика Weighted Round Robin с чтением весов из настроек приложения.
/// </summary>
public class WeightedRoundRobinCreator(IOptions<WeightedRoundRobinOptions> options) : ILoadBalancerCreator
{
    public string Type => nameof(WeightedRoundRobinCreator).Replace("Creator", "");

    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        var services = serviceProvider.GetAsync().Result;
        var hostAndPorts = services.Select(service => service.HostAndPort).ToList();
        var configuredWeights = options.Value.Weights;
        var weights = new int[hostAndPorts.Count];
        for (var i = 0; i < hostAndPorts.Count; i++)
        {
            weights[i] = i < configuredWeights.Length && configuredWeights[i] > 0
                ? configuredWeights[i]
                : 1;
        }

        return new OkResponse<ILoadBalancer>(
            new WeightedRoundRobinLoadBalancer(hostAndPorts, weights));
    }
}
