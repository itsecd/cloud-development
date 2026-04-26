using Aspire.Hosting;
using AspireApp.ApiService.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit.Abstractions;

namespace AspireApp.AppHost.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireApp_AppHost>(cancellationToken);
        _builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        _builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });
    }

    /// <summary>
    /// Проверяет, что вызов гейтвея:
    /// <list type="bullet">
    /// <item><description>В ответ отправляет сгенерированный товар</description></item>
    /// <item><description>Сериализует товар в S3 хранилище через SQS</description></item>
    /// <item><description>Данные из API и S3 идентичны</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task Pipeline_GatewayResponse_MatchesS3Object()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var random = new Random();
        var id = random.Next(1, 100);
        using var gatewayClient = _app.CreateHttpClient("api-gateway", "gateway");
        using var gatewayResponse = await gatewayClient!.GetAsync($"/warehouse?id={id}");
        var apiWarehouse = JsonSerializer.Deserialize<Warehouse>(await gatewayResponse.Content.ReadAsStringAsync());

        await Task.Delay(5000);
        using var fileClient = _app.CreateHttpClient("warehouse-fileservice", "http");
        using var listResponse = await fileClient!.GetAsync($"/api/s3");
        var fileList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());
        using var s3Response = await fileClient!.GetAsync($"/api/s3/warehouse_{id}.json");
        var s3Warehouse = JsonSerializer.Deserialize<Warehouse>(await s3Response.Content.ReadAsStringAsync());

        Assert.NotNull(fileList);
        Assert.Single(fileList);
        Assert.NotNull(apiWarehouse);
        Assert.NotNull(s3Warehouse);
        Assert.Equal(id, s3Warehouse.Id);
        Assert.Equivalent(apiWarehouse, s3Warehouse);
    }

    /// <summary>
    /// Проверяет идемпотентность пайплайна за счёт Redis-кэша:
    /// <list type="bullet">
    /// <item><description>Повторный запрос с тем же id возвращает идентичные данные</description></item>
    /// <item><description>Балансировщик отдаёт запросы разным репликам, но кэш един</description></item>
    /// <item><description>Объект в S3 совпадает с ответом gateway</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task Cache_RepeatedRequests_ReturnSameWarehouse()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var random = new Random();
        var id = random.Next(1, 100);

        using var gatewayClient = _app.CreateHttpClient("api-gateway", "gateway");
        using var firstResponse = await gatewayClient!.GetAsync($"/warehouse?id={id}");
        var firstWarehouse = JsonSerializer.Deserialize<Warehouse>(await firstResponse.Content.ReadAsStringAsync());

        using var secondClient = _app.CreateHttpClient("api-gateway", "gateway");
        using var secondResponse = await secondClient!.GetAsync($"/warehouse?id={id}");
        var secondWarehouse = JsonSerializer.Deserialize<Warehouse>(await secondResponse.Content.ReadAsStringAsync());

        await Task.Delay(5000);
        using var fileClient = _app.CreateHttpClient("warehouse-fileservice", "http");
        using var s3Response = await fileClient!.GetAsync($"/api/s3/warehouse_{id}.json");
        var s3Warehouse = JsonSerializer.Deserialize<Warehouse>(await s3Response.Content.ReadAsStringAsync());

        Assert.NotNull(firstWarehouse);
        Assert.NotNull(secondWarehouse);
        Assert.NotNull(s3Warehouse);
        Assert.Equivalent(firstWarehouse, secondWarehouse);
        Assert.Equivalent(firstWarehouse, s3Warehouse);
    }

    /// <summary>
    /// Проверяет надёжность связки балансировщик → SQS → потребитель:
    /// <list type="bullet">
    /// <item><description>10 запросов распределяются между 3 репликами ApiService</description></item>
    /// <item><description>Все реплики публикуют в одну очередь SQS</description></item>
    /// <item><description>Единственный потребитель FileService обрабатывает поток без потерь</description></item>
    /// <item><description>В S3 оказываются все 10 файлов с ожидаемыми ключами</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task Pipeline_LoadBalancing_AllReplicasProduceToSameQueue()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var ids = Enumerable.Range(100, 10).ToArray();

        foreach (var id in ids)
        {
            using var client = _app.CreateHttpClient("api-gateway", "gateway");
            using var response = await client!.GetAsync($"/warehouse?id={id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        await Task.Delay(10000);

        using var fileClient = _app.CreateHttpClient("warehouse-fileservice", "http");
        using var listResponse = await fileClient!.GetAsync($"/api/s3");
        var fileList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(fileList);
        foreach (var id in ids)
            Assert.Contains($"warehouse_{id}.json", fileList);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
        await _builder!.DisposeAsync();
    }
}
