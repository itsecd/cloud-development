using ApiGateway.Configuration;
using Microsoft.Extensions.Options;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;
using System.Threading;

namespace ApiGateway.LoadBalancing;

public sealed class WeightedRoundRobinBalancer : ILoadBalancer
{
    private readonly ILogger<WeightedRoundRobinBalancer> _logger;
    private readonly List<WeightedNode> _nodes;
    private readonly int _totalWeight;
    private long _requestCounter;

    public WeightedRoundRobinBalancer(
        IOptions<WeightedRoundRobinOptions> options,
        ILogger<WeightedRoundRobinBalancer> logger)
    {
        _logger = logger;
        _nodes = BuildNodes(options.Value.Nodes);

        _totalWeight = _nodes.Sum(static node => node.Weight);

        if (_nodes.Count == 0 || _totalWeight <= 0)
        {
            throw new InvalidOperationException(
                "Не настроены узлы для Weighted Round Robin балансировки.");
        }
    }

    public string Type => nameof(WeightedRoundRobinBalancer);

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var currentRequest = Interlocked.Increment(ref _requestCounter);

        var index = (int)((currentRequest - 1) % _totalWeight);

        WeightedNode? selectedNode = null;
        var cumulativeWeight = 0;

        foreach (var node in _nodes)
        {
            cumulativeWeight += node.Weight;

            if (index < cumulativeWeight)
            {
                selectedNode = node;
                break;
            }
        }

        selectedNode ??= _nodes[^1];

        _logger.LogInformation(
            "Gateway routed request to replica {ReplicaId} ({ReplicaAddress}) by {BalancerType} with weight {ReplicaWeight}",
            selectedNode.ReplicaId,
            selectedNode.HostAndPort,
            Type,
            selectedNode.Weight);

        return Task.FromResult<Response<ServiceHostAndPort>>(
            new OkResponse<ServiceHostAndPort>(selectedNode.HostAndPort));
    }

    public void Release(ServiceHostAndPort hostAndPort)
    {
    }

    private static List<WeightedNode> BuildNodes(IEnumerable<ReplicaNodeOptions>? nodes)
    {
        var result = new List<WeightedNode>();

        if (nodes is null)
        {
            return result;
        }

        foreach (var node in nodes.Where(static n =>
                     !string.IsNullOrWhiteSpace(n.Host) && n.Port > 0))
        {
            var normalizedWeight = Math.Max(1, node.Weight);

            result.Add(new WeightedNode(
                node.ReplicaId,
                new ServiceHostAndPort(node.Host, node.Port),
                normalizedWeight));
        }

        return result;
    }

    private sealed class WeightedNode
    {
        public WeightedNode(string replicaId, ServiceHostAndPort hostAndPort, int weight)
        {
            ReplicaId = string.IsNullOrWhiteSpace(replicaId)
                ? hostAndPort.ToString()
                : replicaId;

            HostAndPort = hostAndPort;
            Weight = weight;
        }

        public string ReplicaId { get; }

        public ServiceHostAndPort HostAndPort { get; }

        public int Weight { get; }
    }
}
