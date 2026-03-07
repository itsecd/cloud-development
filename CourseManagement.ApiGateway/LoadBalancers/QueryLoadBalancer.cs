using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CourseManagement.ApiGateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки на основе запроса
/// </summary>
/// <param name="logger">Логгер</param>
/// <param name="services">Список сервисов</param>
public class QueryLoadBalancer(ILogger<QueryLoadBalancer> logger, List<Service> services) : ILoadBalancer
{
    /// <summary>
    /// Тип балансировщика нагрузки
    /// </summary>
    public string Type => "QueryLoadBalancer";

    /// <summary>
    /// Метод выбора сервиса на основе запроса
    /// </summary>
    /// <param name="httpContext">HTTP запрос</param>
    /// <returns>Выбранный сервис</returns>
    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var idString = httpContext.Request.Query["id"].FirstOrDefault();

        if (!int.TryParse(idString, out var id))
            id = 0;

        var service = services[id % services.Count];

        logger.LogInformation("Request {ResourceId} sent to service on port {servicePort}", id, service.HostAndPort.DownstreamPort);

        return Task.FromResult<Response<ServiceHostAndPort>>(
            new OkResponse<ServiceHostAndPort>(service.HostAndPort));
    }

    /// <summary>
    /// Метод очистки ресурсов для сервиса
    /// </summary>
    /// <param name="hostAndPort">Параметры сервиса</param>
    public void Release(ServiceHostAndPort hostAndPort) { }
}
