using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace VehicleApp.Gateway;


/// <summary>
/// Балансировщик нагрузки на основе параметра запроса.
/// Реплика выбирается по формуле: <c>id % N</c>, где N — количество реплик.
/// </summary>
/// <param name="services">Делегат для получения списка доступных реплик сервиса.</param>
public class QueryBased(Func<Task<List<Service>>> services) : ILoadBalancer
{
    public string Type => nameof(QueryBased);

    /// <summary>Выбирает реплику на основе query-параметра <c>id</c>.</summary>
    /// <param name="context">HTTP-контекст входящего запроса.</param>
    /// <returns>Хост и порт выбранной реплики.</returns>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext context)
    {
        var pool = await services();
        var id = int.TryParse(context.Request.Query["id"], out var v) ? v : 0;
        var target = pool[Math.Abs(id) % pool.Count];
        return new OkResponse<ServiceHostAndPort>(target.HostAndPort);
    }

    public void Release(ServiceHostAndPort hostAndPort) { }
}