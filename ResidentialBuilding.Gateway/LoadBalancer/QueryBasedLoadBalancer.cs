using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ResidentialBuilding.Gateway.LoadBalancer;

/// <summary>
/// Query-based балансировщик нагрузки для Ocelot.
/// Выбирает downstream-сервис на основе хэша от отсортированных query-параметров запроса.
/// </summary>
/// <param name="logger">Логгер.</param>
/// <param name="services">Асинхронная функция, возвращающая актуальный список доступных downstream-сервисов.</param>
public class QueryBasedLoadBalancer(ILogger<QueryBasedLoadBalancer> logger, Func<Task<List<Service>>> services)
    : ILoadBalancer
{
    public string Type => nameof(QueryBasedLoadBalancer);

    /// <summary>
    /// Метод вызывается Ocelot после завершения запроса к downstream-сервису.
    /// В текущей реализации ничего не делает (не требуется для query-based подхода).
    /// </summary>
    public void Release(ServiceHostAndPort hostAndPort) { }
    
    /// <summary>
    /// Основной метод выбора downstream-сервиса на основе query-параметров текущего запроса.
    /// </summary>
    /// <param name="httpContext">Контекст HTTP-запроса (используется для доступа к Query string).</param>
    /// <returns>
    /// OkResponse с выбранным адресом сервиса в зависимости от query-параметров, либо случайно выбранный адрес
    /// сервиса, если query-параметры не заданы.
    /// </returns>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var currentServices = await services.Invoke();
        var query = httpContext.Request.Query;
        
        if (query.Count <= 0)
        {
            var randomIndex = Random.Shared.Next(currentServices.Count);
            logger.LogWarning("Query doesn't contain any query parameters, index={randomIndex} selected by random.", randomIndex);
            
            return new OkResponse<ServiceHostAndPort>(currentServices[randomIndex].HostAndPort);
        }
        
        var queryParams = query
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => $"{kvp.Key}={string.Join(",", kvp.Value.OrderBy(v => v))}")
            .ToList();
        
        var hashKey = string.Join("&", queryParams);
        var hash = hashKey.GetHashCode();
        if (hash < 0)
        {
            hash = -hash;
        }
        var index = (hash % currentServices.Count);
        logger.LogInformation("Query based selected index={index} for hash={hash} calculated for string='{hashKey}'", index, hash, hashKey);
        
        return new OkResponse<ServiceHostAndPort>(currentServices[index].HostAndPort);
    }
}