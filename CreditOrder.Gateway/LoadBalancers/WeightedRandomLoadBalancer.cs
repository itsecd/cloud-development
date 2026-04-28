using Api.Gateway.Models;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.Gateway.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки для Ocelot, работает по алгоритму weighted random.
///
/// Вес каждой реплики задаётся в конфигурации через секцию <c>ReplicaWeights</c>.
///
/// Чем больше вес реплики, тем выше вероятность, что запрос будет направлен именно на неt.
/// </summary>
public sealed class WeightedRandomLoadBalancer(IConfiguration configuration,
        Func<Task<List<Service>>> _services) : ILoadBalancer
{
    /// <summary>
    /// Конфигурация весов реплик downstream-сервисов.
    /// </summary>
    private readonly List<ReplicaWeight> _weights = configuration.GetSection("ReplicaWeights").Get<List<ReplicaWeight>>() ?? [];

    /// <summary>
    /// Тип балансировщика нагрузки.
    /// Используется Ocelot для сопоставления с конфигурацией
    /// <c>LoadBalancerOptions.Type</c>.
    /// </summary>
    public string Type => nameof(WeightedRandomLoadBalancer);

    /// <summary>
    /// lookup весов по паре (Host, Port).
    /// </summary>
    private readonly Dictionary<(string Host, int Port), int> _weightsByEndpoint =
        (configuration.GetSection("ReplicaWeights").Get<List<ReplicaWeight>>() ?? [])
        .ToDictionary(
            w => (w.Host.ToLowerInvariant(), w.Port),
            w => w.Weight);

    /// <summary>
    /// Выбирает downstream-сервис для обработки запроса.
    /// Если веса для сервисов не заданы, используется первый доступный сервис.
    /// </summary>
    /// <param name="_">
    /// HTTP-контекст текущего запроса.
    /// В данном балансировщике не используется.
    /// </param>
    /// <returns>
    /// Объект <see cref="Response{ServiceHostAndPort}"/>,
    /// содержащий выбранный downstream-сервис.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если список downstream-сервисов пуст.
    /// </exception>
    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext _)
    {
        var services = await _services();

        var candidates = new List<(Service Service, int Weight)>();

        foreach (var service in services)
        {
            var key = (
                service.HostAndPort.DownstreamHost.ToLowerInvariant(),
                service.HostAndPort.DownstreamPort
            );

            if (_weightsByEndpoint.TryGetValue(key, out var serviceWeight))
            {
                candidates.Add((service, serviceWeight));
            }
        }

        if (candidates.Count == 0)
        {
            var fallback = services.FirstOrDefault();
            if (fallback is null)
            {
                throw new InvalidOperationException("No downstream services configured.");
            }

            return await Task.FromResult<Response<ServiceHostAndPort>>(
                new OkResponse<ServiceHostAndPort>(fallback.HostAndPort));
        }

        var totalWeight = candidates.Sum(x => x.Weight);
        var resSum = Random.Shared.Next(1, totalWeight + 1);

        var weight = 0;
        foreach (var candidate in candidates)
        {
            weight += candidate.Weight;
            if (resSum <= weight)
            {
                return await Task.FromResult<Response<ServiceHostAndPort>>(
                    new OkResponse<ServiceHostAndPort>(candidate.Service.HostAndPort));
            }
        }

        return await Task.FromResult<Response<ServiceHostAndPort>>(
            new OkResponse<ServiceHostAndPort>(candidates[^1].Service.HostAndPort));
    }

    /// <summary>
    /// Освобождает ранее выделенный сервис.
    ///
    /// В данном балансировщике метод не используется,
    /// так как выбор сервиса происходит без удержания состояния.
    /// </summary>
    /// <param name="_">Адрес сервиса.</param>
    public void Release(ServiceHostAndPort _)
    {
    }
}