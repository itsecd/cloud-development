using Ocelot.Configuration;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;

namespace ProgramProject.Gateway.LoadBalancers;

/// <summary>
/// Генератор (хотя его ещё и называют фабрикой) балансировщиков
/// </summary>
public class QueryBasedLoadBalancerFactory : ILoadBalancerFactory
{
    private readonly ILogger<QueryBasedLoadBalancer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public QueryBasedLoadBalancerFactory(
        ILogger<QueryBasedLoadBalancer> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Response<ILoadBalancer> Get(DownstreamRoute route, ServiceProviderConfiguration serviceProviderConfiguration)
    {
        // Если есть ключ в настройках - берем его, иначе "id"
        var queryParameterName = "id";
        if (route.LoadBalancerOptions?.Key != null)
        {
            queryParameterName = route.LoadBalancerOptions.Key;
        }

        _logger.LogInformation("Создаю балансировщик с параметром: {ParamName}", queryParameterName);

        try
        {
            // Получаем провайдер обнаружения сервисов
            var serviceDiscoveryProvider = _serviceProvider.GetService<IServiceDiscoveryProvider>();

            if (serviceDiscoveryProvider == null)
            {
                _logger.LogError("ServiceDiscoveryProvider не найден");
                return new ErrorResponse<ILoadBalancer>(
                    new UnableToFindServiceDiscoveryProviderError(
                        "ServiceDiscoveryProvider не найден в контейнере"));
            }

            // Получаем список сервисов
            var services = serviceDiscoveryProvider.GetAsync().GetAwaiter().GetResult().ToList();

            if (services.Count == 0)
            {
                _logger.LogWarning("Нет доступных сервисов для балансировки");
                return new ErrorResponse<ILoadBalancer>(
                    new ServicesAreEmptyError("Нет доступных downstream сервисов"));
            }

            _logger.LogInformation("Найдено {Count} сервисов для балансировки", services.Count);

            // Создаем балансировщик
            var loadBalancer = new QueryBasedLoadBalancer(services, _logger, queryParameterName);
            return new OkResponse<ILoadBalancer>(loadBalancer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании балансировщика");
            return new ErrorResponse<ILoadBalancer>(
                new UnableToFindServiceDiscoveryProviderError(
                    $"Ошибка создания балансировщика: {ex.Message}"));
        }
    }
}