using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ProgramProject.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик для алгоритма Query Based
/// Распределяет запросы на основе query-параметра id
/// </summary>
public class QueryBasedLoadBalancer : ILoadBalancer
{
    private readonly ILogger<QueryBasedLoadBalancer> _logger;
    private readonly string _queryParameterName;
    private readonly Func<Task<List<Service>>> _serviceFactory;

    public QueryBasedLoadBalancer(Func<Task<List<Service>>> serviceFactory, ILogger<QueryBasedLoadBalancer> logger,
        string queryParameterName = "id")
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queryParameterName = queryParameterName;
    }

    public string Type => "QueryBased";

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        try
        {
            var services = await _serviceFactory();

            if (services == null || services.Count == 0)
            {
                _logger.LogError("Нет доступных сервисов для балансировки");
                return new ErrorResponse<ServiceHostAndPort>(
                    new ServicesAreEmptyError("Нет доступных реплик"));
            }

            var id = ExtractIdFromQuery(httpContext);

            var index = Math.Abs(id) % services.Count;

            var selectedService = services[index];
            _logger.LogInformation(
                "Запрос с id={Id} (индекс={Index}) направлен на реплику {Host}:{Port}",
                id, index, selectedService.HostAndPort.DownstreamHost, selectedService.HostAndPort.DownstreamPort);

            return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при балансировке запроса");
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreEmptyError($"Ошибка балансировки: {ex.Message}"));
        }
    }

    private int ExtractIdFromQuery(HttpContext context)
    {
        if (context.Request.Query.TryGetValue(_queryParameterName, out var idString))
        {
            if (int.TryParse(idString, out var id))
            {
                _logger.LogDebug("Извлечён id={Id} из запроса", id);
                return id;
            }

            _logger.LogWarning("Параметр {Param} содержит не число: {Value}",
                _queryParameterName, idString);
        }
        else
        {
            _logger.LogDebug("Параметр {Param} отсутствует в запросе", _queryParameterName);
        }

        return 0;
    }

    public void Release(ServiceHostAndPort hostAndPort)
    { }
}