using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace Inventory.Gateway.LoadBalancer;

/// <summary>
/// Пользовательский балансировщик нагрузки для Ocelot,реализующий алгоритм взвешенного случайного выбора (Weighted Random).
/// Каждый сервис получает вес, увеличивающийся в зависимости от его позиции в списке,
/// после чего один из сервисов выбирается случайным образом
/// пропорционально своему весу.
/// </summary>
/// <param name="logger"> Логгер для записи информации о выбранном сервисе</param>
/// <param name="services"> Список доступных сервисов для балансировки нагрузки</param>
public class WeightedRandom(ILogger<WeightedRandom> logger, List<Service> services) : ILoadBalancer
{
    /// <summary>
    /// Тип используемого балансировщика нагрузки.
    /// </summary>
    public string Type => "WeightedRandom";

    /// <summary>
    /// Генератор случайных чисел для выбора сервиса.
    /// </summary>
    private readonly Random _rng = new();

    /// <summary>
    /// Выбирает один из доступных сервисов на основе алгоритма взвешенного случайного выбора.
    /// </summary>
    /// <param name="httpContext">Контекст HTTP-запроса.</param>
    /// <returns>Выбранный сервис (Host и Port).</returns>
    public Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var totalWeight = 0;
        for (var i = 0; i < services.Count; i++)
            totalWeight += i + 1;
        
        var ticket = _rng.Next(totalWeight + 1);

        var cumulative = 0;
        for (var i = 0; i < services.Count; i++)
        {
            var weight = i + 1;

            cumulative += weight;

            if (ticket <= cumulative)
            {
                var service = services[i];

                logger.LogInformation("WeightedRandom selected port {port}", service.HostAndPort.DownstreamPort);

                return Task.FromResult<Response<ServiceHostAndPort>>(
                    new OkResponse<ServiceHostAndPort>(service.HostAndPort));
            }
        }

        var fallback = services.Last();

        return Task.FromResult<Response<ServiceHostAndPort>>(
            new OkResponse<ServiceHostAndPort>(fallback.HostAndPort));
    }

    /// <summary>
    /// Метод освобождения ресурса после использования.
    /// </summary>
    /// <param name="hostAndPort"></param>
    public void Release(ServiceHostAndPort hostAndPort) { }
}