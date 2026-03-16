using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки на основе query-параметра id
/// Определяет реплику по остатку от деления идентификатора на число реплик
/// </summary>
/// <param name="services">Делегат для получения списка доступных реплик</param>
public class QueryBased(Func<Task<List<Service>>> services) : ILoadBalancer
{
    public string Type => nameof(QueryBased);

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var availableServices = await services.Invoke();
        var replicaCount = availableServices.Count;

        var replicaIndex = 0;

        if (httpContext.Request.Query.TryGetValue("id", out var rawId)
            && int.TryParse(rawId, out var employeeId))
        {
            replicaIndex = Math.Abs(employeeId) % replicaCount;
        }

        return new OkResponse<ServiceHostAndPort>(availableServices[replicaIndex].HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}
