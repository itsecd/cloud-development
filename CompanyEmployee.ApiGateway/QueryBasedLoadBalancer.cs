using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace CompanyEmployee.ApiGateway;

/// <summary>
/// Класс для балансировкик нагрузки с использованием параметра запроса.
/// </summary>
/// <param name="services">Функция, возвращающая список downstream-сервисов</param>
/// <param name="logger">Логгер</param>
public class QueryBasedLoadBalancer(Func<Task<List<Service>>> services, ILogger<QueryBasedLoadBalancer> logger) 
    : ILoadBalancer
{
    
    public string Type => nameof(QueryBasedLoadBalancer);
    
    /// <summary>
    /// Метод для выбора downstream-сервиса на основании параметра запроса.
    /// Для выбора используется id из запроса. От него вычисляется остаток от деления на количество downstream-сервисов.
    /// После этого выбирается реплика с соответствующим номером.
    /// Если из запроса не удалось прочитать id, то используется случайный downstream-сервис.
    /// </summary>
    /// <param name="context">Контекст http-запроса</param>
    /// <returns>OkResponse с адресос выбранного сервиса, если получилось выбрать downstream-сервис, или
    /// ErrorResponse, если определить downstream-сервис не удалось
    /// </returns>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var availableServices = await services.Invoke();
        if (availableServices.Count == 0)
        {
            logger.LogError("No services available");
            return new ErrorResponse<ServiceHostAndPort>(
                new UnableToFindDownstreamRouteError(context.Request.Path.Value, context.Request.Method));
        }
        
        var idStr = context.Request.Query["id"].FirstOrDefault("error");
        if (!int.TryParse(idStr, out var index))
        {
            logger.LogWarning($"Could not parse id: {idStr}, random replica will be selected", idStr);
            index = Random.Shared.Next(availableServices.Count);
        }
        
        index %= availableServices.Count;
        if (index < 0)
        {
            index += availableServices.Count;
        }
        
        logger.LogInformation($"Using replica with index: {index}", index);
        
        return new OkResponse<ServiceHostAndPort>(availableServices[index].HostAndPort);
    }

    /// <summary>
    /// Метод предназначен для уведомления о завершении запроса к downstream-сервису.
    /// В query based балансировке не используется.
    /// </summary>
    /// <param name="hostAndPort">Адрес downstream-сервиса</param>
    public void Release(ServiceHostAndPort hostAndPort) { }
}