using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ProgramProject.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик для алгоритма Query Based
/// </summary>
public sealed class QueryBasedLoadBalancer : ILoadBalancer
{
    private readonly List<Service> _services;
    private readonly ILogger<QueryBasedLoadBalancer> _logger;
    private readonly string _queryParameterName;

    public QueryBasedLoadBalancer(
        List<Service> services,
        ILogger<QueryBasedLoadBalancer> logger,
        string queryParameterName = "id")
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queryParameterName = queryParameterName;

        _logger.LogInformation("QueryBasedLoadBalancer инициализирован с {ReplicaCount} репликами", _services.Count);
    }
    public string Type => nameof(QueryBasedLoadBalancer);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        if (_services.Count == 0)
        {
            _logger.LogError("Нет доступных реплик для балансировки");
            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("Нет доступных реплик"));
        }
        try
        {
            var idValue = ExtractIdFromQuery(httpContext);
            var replicaIndex = CalculateReplicaIndex(idValue);
            var selectedService = _services[replicaIndex];

            _logger.LogInformation("Запрос с id={Id} направлен на реплику {ReplicaIndex} ({HostAndPort})",
                idValue, replicaIndex, selectedService.HostAndPort);

            return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при балансировке запроса, использую первую реплику");
            return new OkResponse<ServiceHostAndPort>(_services[0].HostAndPort);
        }
    }

    /// <summary>
    /// Извлекаем значение id
    /// </summary>
    private int ExtractIdFromQuery(HttpContext context)
    {
        if (context.Request.Query.TryGetValue(_queryParameterName, out var idString))
        {
            if (int.TryParse(idString, out var id))
            {
                _logger.LogDebug("Извлечен id={Id} из query параметра {ParamName}", id, _queryParameterName);
                return id;
            }
        }

        _logger.LogWarning("Параметр {ParamName} не найден или не является числом, использую id=0", _queryParameterName);
        return 0;
    }

    /// <summary>
    /// Вычисляет индекс реплики
    /// </summary>
    private int CalculateReplicaIndex(int id)
    {
        var absId = Math.Abs(id);
        var index = absId % _services.Count;

        _logger.LogDebug("Для id={Id} вычислен индекс реплики {Index}", id, index);
        return index;
    }

    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Release(ServiceHostAndPort hostAndPort) {}
}