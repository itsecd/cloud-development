using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway.LoadBalancing;

public class WeightedRoundRobinBalancer : ILoadBalancer
{
    public string Type => "WeightedRoundRobin";

    private readonly List<ServiceHostAndPort> _expandedHosts;
    private readonly object _lock = new();
    private int _currentIndex = 0;

    public WeightedRoundRobinBalancer(IConfiguration configuration)
    {
        var hosts = new List<(string host, int port, int weight)>();
        AddHostFromConfig(configuration, "generation-service-1", 3, hosts);
        AddHostFromConfig(configuration, "generation-service-2", 2, hosts);
        AddHostFromConfig(configuration, "generation-service-3", 1, hosts);

        _expandedHosts = new List<ServiceHostAndPort>();
        foreach (var (host, port, weight) in hosts)
        {
            for (var i = 0; i < weight; i++)
            {
                _expandedHosts.Add(new ServiceHostAndPort(host, port));
            }
        }

        if (_expandedHosts.Count == 0)
        {
            _expandedHosts.Add(new ServiceHostAndPort("localhost", 5001));
        }
    }

    private static void AddHostFromConfig(
        IConfiguration config,
        string serviceName,
        int weight,
        List<(string, int, int)> hosts)
    {
        var url = config[$"services__{serviceName}__http__0"];
        if (string.IsNullOrEmpty(url)) return;

        var uri = new Uri(url);
        hosts.Add((uri.Host, uri.Port, weight));
    }

    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        lock (_lock)
        {
            var host = _expandedHosts[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _expandedHosts.Count;

            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(host));
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}