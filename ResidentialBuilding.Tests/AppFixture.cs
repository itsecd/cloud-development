using Aspire.Hosting;

namespace ResidentialBuilding.Tests;

/// <summary>
/// Фикстура для интеграционных тестов, использующая .NET Aspire DistributedApplicationTesting.
/// Обеспечивает запуск полного стека приложения (AppHost) один раз для всех тестов класса,
/// использующих <see cref="IClassFixture{AppFixture}"/>.
/// </summary>
public class AppFixture : IAsyncLifetime
{
    /// <summary>
    /// Экземпляр запущенного распределённого приложения Aspire.
    /// Доступен всем тестам через внедрение зависимости.
    /// </summary>
    public DistributedApplication App { get; private set; } = null!;
    
    private IDistributedApplicationTestingBuilder? _builder;

    /// <summary>
    /// Инициализирует и запускает все сервисы приложения перед выполнением тестов.
    /// </summary>
    public async Task InitializeAsync()
    {
        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ResidentialBuilding_AppHost>();
        _builder.Configuration["DcpPublisher:RandomizePorts"] = "false";

        App = await _builder.BuildAsync();
        await App.StartAsync();
    }

    /// <summary>
    /// Корректно останавливает все сервисы и освобождает ресурсы после завершения тестов.
    /// </summary>
    public async Task DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
        await _builder!.DisposeAsync();
    }
}