using Ocelot.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CompanyEmployee.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик с взвешенным случайным распределением запросов.
/// </summary>
public class WeightedRandomLoadBalancer : ILoadBalancer
{
    private readonly List<Service> _services;
    private readonly Dictionary<string, int> _weights;
    private readonly Random _random = new();
    private readonly object _lock = new();

    /// <summary>
    /// Тип балансировщика.
    /// </summary>
    public string Type => nameof(WeightedRandomLoadBalancer);

    /// <summary>
    /// Инициализирует новый экземпляр балансировщика с весами.
    /// </summary>
    /// <param name="services">Список сервисов.</param>
    /// <param name="weights">Словарь весов для каждого сервиса.</param>
    public WeightedRandomLoadBalancer(List<Service> services, Dictionary<string, int> weights)
    {
        _services = services;
        _weights = weights;
    }

    /// <inheritdoc />
    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        lock (_lock)
        {
            var weightedList = new List<Service>();

            foreach (var service in _services)
            {
                var key = $"{service.HostAndPort.DownstreamHost}:{service.HostAndPort.DownstreamPort}";
                var weight = _weights.GetValueOrDefault(key, 1);

                for (var i = 0; i < weight; i++)
                {
                    weightedList.Add(service);
                }
            }

            if (weightedList.Count == 0)
            {
                return Task.FromResult<Response<ServiceHostAndPort>>(
                    new ErrorResponse<ServiceHostAndPort>(new List<Error>
                    {
                        new UnableToFindServiceError()
                    }));
            }

            var selected = weightedList[_random.Next(weightedList.Count)];
            return Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(selected.HostAndPort));
        }
    }

    /// <inheritdoc />
    public void Release(ServiceHostAndPort hostAndPort)
    {
    }
}

/// <summary>
/// Ошибка, возникающая когда сервис не найден.
/// </summary>
public class UnableToFindServiceError : Error
{
    public UnableToFindServiceError()
        : base("Нет доступных сервисов для обработки запроса", OcelotErrorCode.UnableToFindDownstreamRouteError, 404)
    {
    }
}