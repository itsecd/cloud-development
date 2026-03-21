using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.LoadBalancing;
using Yarp.ReverseProxy.Model;

namespace AspireApp.ApiGateway.LoadBalancing;

/// <summary>
/// Weighted Random балансировщик нагрузки.
/// Распределяет запросы между сервисами по заданным весам.
/// </summary>

public class WeightedRandomLoadBalancer : ILoadBalancingPolicy
{
    private readonly Random _random = new();

    public string Name => "WeightedRandom";

    public DestinationState? PickDestination(HttpContext context, ClusterState cluster, IReadOnlyList<DestinationState> destinations)
    {
        if (destinations.Count == 0)
            return null;

        var weightedList = new List<DestinationState>();
        for (var i = 0; i < destinations.Count; i++)
        {
            var weight = i == 0 ? 5 : (i == 1 ? 3 : 2);
            for (var w = 0; w < weight; w++)
            {
                weightedList.Add(destinations[i]);
            }
        }

        var index = _random.Next(weightedList.Count);
        return weightedList[index];
    }
}