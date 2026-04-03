using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Vehicle.Gateway.LoadBalancing;

/// <summary>
/// Кастомный балансировщик нагрузки для Ocelot.
/// Реализует алгоритм Weighted Random через расширенный пул реплик: чем больше вес реплики, тем чаще она попадает в пул выбора.
/// </summary>
/// <remarks>
/// Создаёт балансировщик и загружает веса реплик из конфигурации.
/// </remarks>
/// <param name="configuration">Конфигурация приложения.</param>
/// <param name="getServices">Функция получения доступных downstream-сервисов.</param>
public sealed class WeightedRandomLoadBalancer(IConfiguration configuration, Func<Task<List<Service>>> getServices) : ILoadBalancer
{
    private readonly Dictionary<(string Host, int Port), int> _weights = ReadWeights(configuration);

    /// <summary>
    /// Имя балансировщика. 
    /// </summary>
    public string Type => nameof(WeightedRandomLoadBalancer);

    /// <summary>
    /// Выбирает downstream-сервис для текущего запроса.
    /// </summary>
    /// <param name="_">HTTP-контекст запроса.</param>
    /// <returns>Выбранный адрес downstream-сервиса.</returns>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext _)
    {
        var services = await getServices();

        if (services.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new UnableToFindLoadBalancerError("No downstream services available"));
        }

        var selectionPool = BuildSelectionPool(services);

        if (selectionPool.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new UnableToFindLoadBalancerError("No services were added to the weighted selection pool"));
        }

        var randomIndex = Random.Shared.Next(selectionPool.Count);
        var selected = selectionPool[randomIndex];

        return new OkResponse<ServiceHostAndPort>(selected);
    }

    /// <summary>
    /// Освобождение ресурса 
    /// </summary>
    /// <param name="_">Адрес downstream-сервиса.</param>
    public void Release(ServiceHostAndPort _) { }

    /// <summary>
    /// Строит расширенный пул выбора: каждая реплика добавляется в список столько раз, сколько равен её вес.
    /// </summary>
    /// <param name="services">Список доступных downstream-сервисов.</param>
    /// <returns>Расширенный пул для случайного выбора.</returns>
    private List<ServiceHostAndPort> BuildSelectionPool(IEnumerable<Service> services)
    {
        var pool = new List<ServiceHostAndPort>();

        foreach (var service in services)
        {
            var key = (
                service.HostAndPort.DownstreamHost.ToLowerInvariant(),
                service.HostAndPort.DownstreamPort
            );

            var weight = _weights.GetValueOrDefault(key, 1);
            weight = Math.Max(weight, 1);

            for (var i = 0; i < weight; i++)
            {
                pool.Add(service.HostAndPort);
            }
        }

        return pool;
    }

    /// <summary>
    /// Читает веса реплик из секции WeightedRandom:Replicas.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Словарь весов реплик.</returns>
    private static Dictionary<(string Host, int Port), int> ReadWeights(IConfiguration configuration)
    {
        return configuration
            .GetSection("WeightedRandom:Replicas")
            .GetChildren()
            .Where(item => !string.IsNullOrWhiteSpace(item.Key) && int.TryParse(item.Value, out _))
            .ToDictionary(
                item =>
                {
                    var parts = item.Key.Split(':', 2, StringSplitOptions.TrimEntries);

                    if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
                    {
                        throw new InvalidOperationException(
                            $"Invalid replica endpoint format: '{item.Key}'. Expected format: host:port");
                    }

                    return (parts[0].ToLowerInvariant(), port);
                },
                item => Math.Max(int.Parse(item.Value!), 1)
            );
    }
}