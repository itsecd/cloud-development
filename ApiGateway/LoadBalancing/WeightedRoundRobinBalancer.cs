using ApiGateway.Configuration;
using Microsoft.Extensions.Options;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway.LoadBalancing;

public sealed class WeightedRoundRobinBalancer : ILoadBalancer
{
    private static readonly object Sync = new();
    private readonly ILogger<WeightedRoundRobinBalancer> _logger;
    private readonly List<ServiceHostAndPort> _rotation;
    private int _currentIndex;

    public WeightedRoundRobinBalancer(
        IOptions<WeightedRoundRobinOptions> options,
        ILogger<WeightedRoundRobinBalancer> logger)
    {
        _logger = logger;
        _rotation = BuildRotation(options.Value.Nodes);

        if (_rotation.Count == 0)
        {
            throw new InvalidOperationException("Не настроены узлы для Weighted Round Robin балансировки.");
        }
    }

    public string Type => nameof(WeightedRoundRobinBalancer);

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        lock (Sync)
        {
            if (_currentIndex >= _rotation.Count)
            {
                _currentIndex = 0;
            }

            var next = _rotation[_currentIndex++];

            _logger.LogInformation(
                "Gateway routed request to {ReplicaAddress} by {BalancerType}",
                next,
                Type);

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(next));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }

    private static List<ServiceHostAndPort> BuildRotation(IEnumerable<ReplicaNodeOptions> nodes)
    {
        var rotation = new List<ServiceHostAndPort>();

        foreach (var node in nodes.Where(static n => !string.IsNullOrWhiteSpace(n.Host) && n.Port > 0))
        {
            var normalizedWeight = Math.Max(1, node.Weight);

            for (var i = 0; i < normalizedWeight; i++)
            {
                rotation.Add(new ServiceHostAndPort(node.Host, node.Port));
            }
        }

        return rotation;
    }
}