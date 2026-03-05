using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ResidentialBuilding.Gateway.LoadBalancer;

/// <summary>
/// Query-based балансировщик нагрузки для Ocelot.
/// Выбирает downstream-сервис на основе query-параметра id запроса.
/// </summary>
/// <param name="logger">Логгер.</param>
/// <param name="services">Асинхронная функция, возвращающая актуальный список доступных downstream-сервисов.</param>
public class QueryBasedLoadBalancer(ILogger<QueryBasedLoadBalancer> logger, Func<Task<List<Service>>> services)
    : ILoadBalancer
{
    private const string IdQueryParamName = "id";
    
    public string Type => nameof(QueryBasedLoadBalancer);

    /// <summary>
    /// Метод вызывается Ocelot после завершения запроса к downstream-сервису.
    /// В текущей реализации ничего не делает (не требуется для query-based подхода).
    /// </summary>
    public void Release(ServiceHostAndPort hostAndPort) { }
    
    /// <summary>
    /// Основной метод выбора downstream-сервиса на основе query-параметра id текущего запроса.
    /// </summary>
    /// <param name="httpContext">Контекст HTTP-запроса (используется для доступа к Query string).</param>
    /// <returns>
    /// OkResponse с выбранным адресом сервиса в зависимости от query-параметра id, либо случайно выбранный адрес
    /// сервиса, если query-параметр не задан.
    /// </returns>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var currentServices = await services.Invoke();
        var query = httpContext.Request.Query;
        
        if (!query.TryGetValue(IdQueryParamName, out var idValues) || idValues.Count <= 0)
        {
            return SelectRandomService(currentServices);
        }
        
        var idStr = idValues.First();
        
        if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out var id) || id < 0)
        {
            return SelectRandomService(currentServices);
        }
        
        var index = id % currentServices.Count;
        logger.LogInformation("Query based selected index={index}.", index);
        
        return new OkResponse<ServiceHostAndPort>(currentServices[index].HostAndPort);
    }

    private OkResponse<ServiceHostAndPort> SelectRandomService(List<Service> currentServices)
    {
        var randomIndex = Random.Shared.Next(currentServices.Count);
        logger.LogWarning("Query doesn't contain correct id parameter, index={randomIndex} selected by random.", randomIndex);
            
        return new OkResponse<ServiceHostAndPort>(currentServices[randomIndex].HostAndPort);
    }
}