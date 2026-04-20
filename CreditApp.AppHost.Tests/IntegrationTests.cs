using Aspire.Hosting;
using CreditApp.Domain.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit.Abstractions;

namespace CreditApp.AppHost.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private IDistributedApplicationTestingBuilder _builder = default!;
    private DistributedApplication _app = default!;

    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CreditApp_AppHost>(cancellationToken);
        _builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        _builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        _app = await _builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("gateway", cancellationToken);
    }

    /// <summary>
    /// Проверяет, что вызов через гейтвей возвращает сгенерированную кредитную заявку с запрошенным идентификатором
    /// </summary>
    [Fact]
    public async Task Gateway_ReturnsGeneratedCredit()
    {
        var id = Random.Shared.Next(1, 1000);
        using var client = _app.CreateHttpClient("gateway", "https");
        using var response = await client.GetAsync($"/api/Credit?id={id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var credit = JsonSerializer.Deserialize<CreditApplication>(
            await response.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.NotNull(credit);
        Assert.Equal(id, credit.Id);
    }

    /// <summary>
    /// Проверяет, что повторный запрос за той же заявкой возвращает идентичный результат — сработал кеш Redis
    /// </summary>
    [Fact]
    public async Task Gateway_RepeatedRequest_ReturnsSameCredit()
    {
        var id = Random.Shared.Next(1000, 2000);
        using var client = _app.CreateHttpClient("gateway", "https");

        using var first = await client.GetAsync($"/api/Credit?id={id}");
        using var second = await client.GetAsync($"/api/Credit?id={id}");

        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();

        var firstCredit = JsonSerializer.Deserialize<CreditApplication>(await first.Content.ReadAsStringAsync(), _jsonOptions);
        var secondCredit = JsonSerializer.Deserialize<CreditApplication>(await second.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(firstCredit);
        Assert.NotNull(secondCredit);
        Assert.Equivalent(firstCredit, secondCredit);
    }

    /// <summary>
    /// Проверяет, что несколько последовательных запросов к гейтвею успешно балансируются на реплики API
    /// </summary>
    [Fact]
    public async Task Gateway_LoadBalancing_AllReplicasRespond()
    {
        using var client = _app.CreateHttpClient("gateway", "https");
        for (var i = 1; i <= 10; i++)
        {
            using var response = await client.GetAsync($"/api/Credit?id={i}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    /// <summary>
    /// Проверяет сквозной сценарий: запрос к гейтвею генерирует заявку, API публикует её в SQS,
    /// FileService читает очередь и кладёт файл в S3 под ключом credit_{id}.json
    /// </summary>
    [Fact]
    public async Task Gateway_GeneratesCredit_AndStoresInS3()
    {
        var cancellationToken = CancellationToken.None;
        var id = Random.Shared.Next(2000, 3000);
        var expectedKey = $"credit_{id}.json";

        using var gatewayClient = _app.CreateHttpClient("gateway", "https");
        using var gatewayResponse = await gatewayClient.GetAsync($"/api/Credit?id={id}", cancellationToken);
        gatewayResponse.EnsureSuccessStatusCode();
        var gatewayCredit = JsonSerializer.Deserialize<CreditApplication>(
            await gatewayResponse.Content.ReadAsStringAsync(cancellationToken), _jsonOptions);

        using var fileServiceClient = _app.CreateHttpClient("fileservice", "https");

        CreditApplication? s3Credit = null;
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            using var s3Response = await fileServiceClient.GetAsync($"/api/s3/{expectedKey}", cancellationToken);
            if (s3Response.IsSuccessStatusCode)
            {
                s3Credit = JsonSerializer.Deserialize<CreditApplication>(
                    await s3Response.Content.ReadAsStringAsync(cancellationToken), _jsonOptions);
                break;
            }
            await Task.Delay(1000, cancellationToken);
        }

        using var listResponse = await fileServiceClient.GetAsync("/api/s3", cancellationToken);
        listResponse.EnsureSuccessStatusCode();
        var keys = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync(cancellationToken), _jsonOptions);

        Assert.NotNull(gatewayCredit);
        Assert.NotNull(s3Credit);
        Assert.NotNull(keys);
        Assert.Contains(expectedKey, keys);
        Assert.Equal(id, s3Credit.Id);
        Assert.Equivalent(gatewayCredit, s3Credit);
    }

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _builder.DisposeAsync();
    }
}
