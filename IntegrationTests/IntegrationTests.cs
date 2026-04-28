using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Service.Api.Dto;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private DistributedApplication? _app;
    private CancellationToken _cancellationToken;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudDevelopment_AppHost>(_cancellationToken);
        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        builder.Environment.EnvironmentName = "Development";
        _app = await builder.BuildAsync(_cancellationToken);
        await _app.StartAsync(_cancellationToken);
    }

    /// <summary>
    /// Проверяет, что вызов гейтвея:
    /// <list type="bullet">
    /// <item><description>В ответ отправляет сгенерированный ЗУ</description></item>
    /// <item><description>Сериализует ЗУ в S3 хранилище</description></item>
    /// <item><description>Проверяет, что данные из предыдущих пунктов идентичны</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task TestPipeline()
    {
        var random = new Random();
        var id = random.Next(1, 100);
        using var gatewayClient = _app.CreateHttpClient("gateway", "http");
        using var gatewayResponse = await gatewayClient!.GetAsync($"/api/orders?id={id}");
        var api = await gatewayResponse.Content.ReadFromJsonAsync<CreditOrderDto>(cancellationToken: _cancellationToken);

        await Task.Delay(5000);
        using var sinkClient = _app.CreateHttpClient("service-storage", "http");
        using var listResponse = await sinkClient!.GetAsync($"/api/s3");
        var ppList = await listResponse.Content.ReadFromJsonAsync<List<string>>(cancellationToken: _cancellationToken);
        using var s3Response = await sinkClient!.GetAsync($"/api/s3/credit-order_{id}.json");
        var s3 = await s3Response.Content.ReadFromJsonAsync<CreditOrderDto>(cancellationToken: _cancellationToken);

        Assert.NotNull(ppList);
        Assert.Single(ppList);
        Assert.NotNull(api);
        Assert.NotNull(s3);
        Assert.Equal(id, s3.Id);
        Assert.Equivalent(api, s3);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
    }
}
