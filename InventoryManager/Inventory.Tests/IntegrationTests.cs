using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Inventory.ApiService.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

namespace Inventory.Tests;

/// <summary>
/// Интеграционные тесты для проверки полного сценария генерации инвентаря,
/// публикации сообщения в SNS и сохранения результата в S3
/// </summary>
/// <param name="output">Объект для вывода логов теста в xUnit</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    /// <summary>
    /// Настройки десериализации JSON с нечувствительностью к регистру имён свойств
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    /// <summary>
    /// Инициализирует тестовое распределённое приложение Aspire и настраивает логирование
    /// </summary>
    /// <returns>Асинхронная операция инициализации тестовой среды</returns>
    public async Task InitializeAsync()
    {
        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Inventory_AppHost>();

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
    /// Проверяет, что запрос генерации инвентаря через API Gateway публикует сообщение в SNS
    /// и сохраняет полученный продукт в S3-хранилище
    /// </summary>
    /// <returns>Асинхронная операция выполнения интеграционного теста</returns>
    [Fact]
    public async Task GenerateInventory_ThroughGateway_ShouldPublishToSns_AndSaveToS3()
    {
        Assert.NotNull(_builder);

        _app = await _builder.BuildAsync();
        await _app.StartAsync();

        var id = Random.Shared.Next(1, 10_000);

        using var gatewayClient = _app.CreateHttpClient("apigateway", "https");
        using var gatewayResponse = await gatewayClient.GetAsync($"/api/inventory?id={id}");

        var gatewayContent = await gatewayResponse.Content.ReadAsStringAsync();

        Assert.True(
            gatewayResponse.IsSuccessStatusCode,
            $"Gateway failed: {(int)gatewayResponse.StatusCode} {gatewayResponse.StatusCode}. Body: {gatewayContent}");

        var apiProduct = JsonSerializer.Deserialize<Product>(gatewayContent, _jsonOptions);

        Assert.NotNull(apiProduct);
        Assert.Equal(id, apiProduct.Id);

        using var fileServiceClient = _app.CreateHttpClient("inventory-files", "http");

        var matchingFile = await WaitUntilInventoryFileAppearsAsync(
            fileServiceClient,
            id,
            timeout: TimeSpan.FromSeconds(30));

        Assert.False(string.IsNullOrWhiteSpace(matchingFile));

        using var s3Response = await fileServiceClient.GetAsync($"/api/s3/{matchingFile}");
        var s3Content = await s3Response.Content.ReadAsStringAsync();

        Assert.True(
            s3Response.IsSuccessStatusCode,
            $"S3 read failed: {(int)s3Response.StatusCode} {s3Response.StatusCode}. Body: {s3Content}");

        var s3Product = JsonSerializer.Deserialize<Product>(s3Content, _jsonOptions);

        Assert.NotNull(s3Product);
        Assert.Equal(id, s3Product.Id);
        Assert.Equivalent(apiProduct, s3Product);
    }

    /// <summary>
    /// Ожидает появления файла инвентаря в S3-хранилище в течение заданного времени
    /// </summary>
    /// <param name="fileServiceClient">HTTP-клиент сервиса файлов для обращения к S3 API</param>
    /// <param name="id">Идентификатор продукта, файл которого необходимо найти</param>
    /// <param name="timeout">Максимальное время ожидания появления файла</param>
    /// <returns>Имя найденного файла инвентаря</returns>
    /// <exception cref="TimeoutException">
    /// Возникает, если файл с указанным идентификатором не появился в S3 за отведённое время
    /// </exception>
    private static async Task<string> WaitUntilInventoryFileAppearsAsync(HttpClient fileServiceClient, int id, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var expectedPart = $"inventory_{id}";

        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var listResponse = await fileServiceClient.GetAsync("/api/s3");
                var listContent = await listResponse.Content.ReadAsStringAsync();

                if (listResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    await Task.Delay(500);
                    continue;
                }

                listResponse.EnsureSuccessStatusCode();

                var files = JsonSerializer.Deserialize<List<string>>(listContent, _jsonOptions) ?? [];

                var matchingFile = files.FirstOrDefault(file => file.Contains(expectedPart));

                if (!string.IsNullOrWhiteSpace(matchingFile))
                {
                    return matchingFile;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Inventory file '{expectedPart}' was not found in S3 within {timeout.TotalSeconds} seconds.",
            lastException);
    }

    /// <summary>
    /// Останавливает и освобождает ресурсы тестового распределённого приложения Aspire
    /// </summary>
    /// <returns>Асинхронная операция освобождения ресурсов тестовой среды</returns>
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        if (_builder is not null)
        {
            await _builder.DisposeAsync();
        }
    }
}