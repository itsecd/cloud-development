using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace MedicalPatient.ApiGateway.Balancer;

public class WeightedRoundRobin(IServiceDiscoveryProvider serviceDiscovery, IReadOnlyList<int>? weights = null)
    : ILoadBalancer
{
    private readonly int[] _weights = (weights is { Count: > 0 } ? [.. weights] : []);
    private int _cursor = -1;

    private List<int>? _wheel;
    private int _lastServicesCount = -1;

    public string Type => nameof(WeightedRoundRobin);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await serviceDiscovery.GetAsync();

        if (services is null)
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreNullError("Service discovery returned null"));

        if (services.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreNullError("No downstream services are available"));

        if (_wheel == null || _lastServicesCount != services.Count)
        {
            _wheel = BuildWheel(services.Count);
            _lastServicesCount = services.Count;
        }

        var next = (uint)Interlocked.Increment(ref _cursor);
        var wheelIndex = (int)(next % (uint)_wheel.Count);
        var serviceIndex = _wheel[wheelIndex];

        return new OkResponse<ServiceHostAndPort>(services[serviceIndex].HostAndPort);
    }

    private List<int> BuildWheel(int servicesCount)
    {
        var wheel = new List<int>();

        for (var i = 0; i < servicesCount; i++)
        {
            var weight = i < _weights.Length ? _weights[i] : 1;
            if (weight < 1) weight = 1;

            for (var j = 0; j < weight; j++)
                wheel.Add(i);
        }

        if (wheel.Count == 0)
            wheel.Add(0);

        return wheel;
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
