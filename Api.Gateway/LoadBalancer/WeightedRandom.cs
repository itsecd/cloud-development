using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancer;

/// <summary>
/// Балансировка случайным образом с весами
/// </summary>
public class WeightedRandom : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _services = null!;
    private static readonly object _locker = new();

    private readonly int[] _values = null!;

    public string Type => nameof(WeightedRandom);
    public WeightedRandom(Func<Task<List<Service>>> services, IConfiguration configuration)
    {
        _services = services;
        var frequencies = configuration.GetSection("LoadBalancer:WeightedRandom:Weights").Get<int[]>();
        if (frequencies == null || frequencies.Length == 0)
            throw new InvalidOperationException("Weights is empty or null. Add weights to configuration");
        _values = [.. Enumerable.Range(0, frequencies.Length).Zip(frequencies, (val, freq) => Enumerable.Repeat(val, freq)).SelectMany(x => x)];
    }
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _services.Invoke();
        lock (_locker)
        {
            Random.Shared.Shuffle(_values);
            return new OkResponse<ServiceHostAndPort>(services[_values.First()].HostAndPort);
        }
    }
    public void Release(ServiceHostAndPort hostAndPort) { }
}