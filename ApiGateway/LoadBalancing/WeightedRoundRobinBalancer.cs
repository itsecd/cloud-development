using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway.LoadBalancing;

/// <summary>
/// Взвешенная карусель (Weighted Round Robin).
/// Каждой реплике присваивается вес — она обслуживает ровно weight запросов подряд,
/// после чего очередь переходит к следующей реплике.
/// Веса: R1=3, R2=2, R3=1 → R1,R1,R1,R2,R2,R3,R1,...
/// </summary>
public sealed class WeightedRoundRobinBalancer(Func<Task<List<Service>>> services) : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _services = services;
    private readonly int[] _weights = [3, 2, 1];
    private readonly object _lock = new();

    private int _currentIndex = -1;
    private int _remainingCalls = 0;

    public string Type => nameof(WeightedRoundRobinBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var available = await _services.Invoke();

        if (available is null || available.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreEmptyError("No downstream services available"));

        lock (_lock)
        {
            if (_currentIndex == -1 || _remainingCalls == 0)
            {
                _currentIndex = (_currentIndex + 1) % available.Count;
                _remainingCalls = _weights[_currentIndex % _weights.Length];
            }

            var service = available[_currentIndex];
            _remainingCalls--;

            return new OkResponse<ServiceHostAndPort>(service.HostAndPort);
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
