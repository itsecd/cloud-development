using System.Text.Json;
using Ocelot.Configuration;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace ProjectApp.Gateway.LoadBalancer;

/// <summary>
/// Создатель балансировщика Weighted Round Robin с чтением весов из ocelot.json.
/// </summary>
public class WeightedRoundRobinCreator : ILoadBalancerCreator
{
    public string Type => nameof(WeightedRoundRobinCreator).Replace("Creator", "");

    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        var services = serviceProvider.GetAsync().Result;
        var hostAndPorts = services.Select(service => service.HostAndPort).ToList();

        var weights = new List<int>();
        try
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "ocelot.json");
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);

            var routes = doc.RootElement.GetProperty("Routes");

            foreach (var currentRoute in routes.EnumerateArray())
            {
                if (!currentRoute.TryGetProperty("LoadBalancerOptions", out var options) ||
                    !options.TryGetProperty("Type", out var type) ||
                    !string.Equals(type.GetString(), "WeightedRoundRobin", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var downstream = currentRoute.GetProperty("DownstreamHostAndPorts");
                foreach (var hostPort in downstream.EnumerateArray())
                {
                    var weight = 1;
                    if (hostPort.TryGetProperty("Metadata", out var metadata) &&
                        metadata.TryGetProperty("weight", out var weightElement))
                    {
                        if (weightElement.ValueKind == JsonValueKind.String)
                        {
                            int.TryParse(weightElement.GetString(), out weight);
                        }
                        else if (weightElement.ValueKind == JsonValueKind.Number)
                        {
                            weightElement.TryGetInt32(out weight);
                        }
                    }

                    weights.Add(weight <= 0 ? 1 : weight);
                }

                break;
            }
        }
        catch
        {
        }

        if (weights.Count == 0)
        {
            weights = Enumerable.Repeat(1, hostAndPorts.Count).ToList();
        }

        return new OkResponse<ILoadBalancer>(
            new WeightedRoundRobinLoadBalancer(hostAndPorts, weights.ToArray()));
    }
}
