using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancer;

/// <summary>
/// Балансировщик нагрузки, распределяющий запросы между сервисами с учетом заданных весов.
/// </summary>
/// <param name="services">Функция получения списка доступных сервисов</param>
public class WeightedRoundRobin(Func<Task<List<Service>>> services) : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _services = services;
    private readonly int[] _weights = [1, 2, 3, 2, 1];
    private readonly object _lock = new();
    private int _currentIndex = -1;
    private int _remainingCalls = 0;

    public string Type => nameof(WeightedRoundRobin);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _services.Invoke();
        if (services == null || services.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(
                new Ocelot.LoadBalancer.Errors.ServicesAreEmptyError("No services available"));

        lock (_lock)
        {
            if (_currentIndex >= services.Count)
            {
                _currentIndex = -1;
                _remainingCalls = 0;
            }

            if (_currentIndex == -1 || _remainingCalls == 0)
            {
                _currentIndex = (_currentIndex + 1) % services.Count;
                _remainingCalls = _weights[_currentIndex % _weights.Length];
            }

            var selectedService = services[_currentIndex];
            _remainingCalls--;

            return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
