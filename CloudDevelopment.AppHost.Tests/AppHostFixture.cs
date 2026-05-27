using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudDevelopment.AppHost.Tests;

/// <summary>
/// Фикстура для запуска Aspire-окружения и подготовки HTTP-клиентов тестируемых сервисов.
/// </summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder? _builder;

    /// <summary>
    /// Запущенное тестовое Aspire-приложение.
    /// </summary>
    public DistributedApplication App { get; private set; } = default!;

    /// <summary>
    /// HTTP-клиент API Gateway.
    /// </summary>
    public HttpClient GatewayClient { get; private set; } = default!;

    /// <summary>
    /// HTTP-клиент файлового сервиса.
    /// </summary>
    public HttpClient FileServiceClient { get; private set; } = default!;

    /// <summary>
    /// Поднимает AppHost, ожидает инфраструктурные ресурсы и создает клиенты сервисов.
    /// </summary>
    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudDevelopment_AppHost>(cts.Token);
        _builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        _builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Warning);
            logging.AddFilter("Aspire.Hosting", LogLevel.Information);
        });

        App = await _builder.BuildAsync(cts.Token);
        await App.StartAsync(cts.Token);

        await Task.WhenAll(
            App.ResourceNotifications.WaitForResourceAsync("localstack"),
            App.ResourceNotifications.WaitForResourceAsync("redis"),
            App.ResourceNotifications.WaitForResourceAsync("api-gateway"),
            App.ResourceNotifications.WaitForResourceAsync("file-service")
        ).WaitAsync(cts.Token);

        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);

        GatewayClient = App.CreateHttpClient("api-gateway", "http");
        FileServiceClient = App.CreateHttpClient("file-service", "http");
    }

    /// <summary>
    /// Останавливает Aspire-приложение и освобождает HTTP-клиенты.
    /// </summary>
    public async Task DisposeAsync()
    {
        GatewayClient?.Dispose();
        FileServiceClient?.Dispose();

        if (App is not null)
        {
            await App.StopAsync();
            await App.DisposeAsync();
        }

        if (_builder is not null)
        {
            await _builder.DisposeAsync();
        }
    }
}
