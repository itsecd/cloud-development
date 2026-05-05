using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vehicle.Api.Entities;
using Xunit.Abstractions;

namespace Vehicle.AppHost.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна Vehicle:
/// </summary>
/// <param name="output">Объект для вывода логов теста в xUnit.</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private DistributedApplication? _app;
    private DistributedApplication App => _app ?? throw new InvalidOperationException("Test application was not initialized.");
    private HttpClient CreateGatewayClient() => App.CreateHttpClient("vehicle-gateway", "vehicle-gateway-lb");
    private HttpClient CreateEventSinkClient() => App.CreateHttpClient("vehicle-event-sink", "event-sink-http");

    /// <summary>
    /// Инициализирует тестовое распределенное приложение Aspire, запускает все сервисы и подготавливает HTTP-клиенты.
    /// </summary>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Vehicle_AppHost>(cancellationToken);

        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";

        builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var eventSinkClient = CreateEventSinkClient();

        await WaitUntilEventSinkIsReadyAsync(
            eventSinkClient,
            TimeSpan.FromSeconds(60),
            cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
    }

    /// <summary>
    /// Основной сценарий: запрос через Gateway генерирует транспортное средство, публикует его через SNS, а Vehicle.EventSink сохраняет JSON-файл в Minio.
    /// </summary>
    [Fact]
    public async Task TestPipeline()
    {
        var cancellationToken = CancellationToken.None;

        var gatewayClient = CreateGatewayClient();

        var eventSinkClient = CreateEventSinkClient();

        var id = Random.Shared.Next(100, 100_000);

        using var gatewayResponse = await gatewayClient.GetAsync($"/gateway/Vehicles?id={id}", cancellationToken);

        var gatewayContent = await gatewayResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            gatewayResponse.IsSuccessStatusCode,
            $"Gateway returned {(int)gatewayResponse.StatusCode}: {gatewayContent}");

        var apiVehicle = JsonSerializer.Deserialize<VehicleEntity>(gatewayContent, _jsonOptions);

        Assert.NotNull(apiVehicle);
        Assert.Equal(id, apiVehicle.Id);

        var vehicleFileName = await WaitUntilVehicleFileAppearsAsync(eventSinkClient, id, TimeSpan.FromSeconds(90), cancellationToken);

        using var s3Response = await eventSinkClient.GetAsync($"/api/s3/{vehicleFileName}", cancellationToken);

        var s3Content = await s3Response.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            s3Response.IsSuccessStatusCode,
            $"EventSink returned {(int)s3Response.StatusCode}: {s3Content}");

        var s3Vehicle = JsonSerializer.Deserialize<VehicleEntity>(s3Content, _jsonOptions);

        Assert.NotNull(s3Vehicle);
        Assert.Equal(id, s3Vehicle.Id);
        Assert.Equivalent(apiVehicle, s3Vehicle);
    }

    /// <summary>
    /// Дополнительный важный сценарий: повторный запрос с тем же id возвращается из кэша
    /// и не создает второй файл в Minio для того же транспортного средства.
    /// </summary>
    [Fact]
    public async Task RepeatedRequest_DoesNotCreateDuplicateFileInMinio()
    {
        var cancellationToken = CancellationToken.None;

        var gatewayClient = CreateGatewayClient();

        var eventSinkClient = CreateEventSinkClient();

        var id = Random.Shared.Next(100, 100_000);

        using var firstResponse = await gatewayClient.GetAsync($"/gateway/Vehicles?id={id}", cancellationToken);

        var firstContent = await firstResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            firstResponse.IsSuccessStatusCode,
            $"First gateway request failed: {firstContent}");

        var firstVehicle = JsonSerializer.Deserialize<VehicleEntity>(firstContent, _jsonOptions);

        Assert.NotNull(firstVehicle);

        var firstFileName = await WaitUntilVehicleFileAppearsAsync(eventSinkClient, id, TimeSpan.FromSeconds(90), cancellationToken);

        using var secondResponse = await gatewayClient.GetAsync($"/gateway/Vehicles?id={id}", cancellationToken);

        var secondContent = await secondResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            secondResponse.IsSuccessStatusCode,
            $"Second gateway request failed: {secondContent}");

        var secondVehicle = JsonSerializer.Deserialize<VehicleEntity>(secondContent, _jsonOptions);

        Assert.NotNull(secondVehicle);
        Assert.Equivalent(firstVehicle, secondVehicle);

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        var files = await GetFileListAsync(eventSinkClient, cancellationToken);

        var filesForCurrentId = files
            .Where(file => file.StartsWith($"vehicle_{id}_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Single(filesForCurrentId);
        Assert.Equal(firstFileName, filesForCurrentId[0]);
    }

    /// <summary>
    /// Ждет, пока Vehicle.EventSink начнет отвечать на запросы к /api/s3
    /// </summary>
    private static async Task WaitUntilEventSinkIsReadyAsync(HttpClient eventSinkClient, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        Exception? lastException = null;
        var lastResponse = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await eventSinkClient.GetAsync("/api/s3", cancellationToken);

                lastResponse = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException($"Vehicle.EventSink did not become ready within {timeout.TotalSeconds} seconds. Last response: {lastResponse}", lastException);
    }

    /// <summary>
    /// Ожидает появления JSON-файла vehicle_{id}_*.json в Minio
    /// </summary>
    private static async Task<string> WaitUntilVehicleFileAppearsAsync(HttpClient eventSinkClient, int vehicleId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var expectedPrefix = $"vehicle_{vehicleId}_";

        Exception? lastException = null;
        var lastFileList = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var files = await GetFileListAsync(eventSinkClient, cancellationToken);
                lastFileList = JsonSerializer.Serialize(files, _jsonOptions);

                var matchingFile = files.FirstOrDefault(file => file.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(matchingFile))
                    return matchingFile;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException(
            $"File with prefix '{expectedPrefix}' was not found in Minio within {timeout.TotalSeconds} seconds. Last file list: {lastFileList}",
            lastException);
    }

    /// <summary>
    /// Получает список файлов из Minio через Vehicle.EventSink
    /// </summary>
    private static async Task<List<string>> GetFileListAsync(HttpClient eventSinkClient, CancellationToken cancellationToken)
    {
        using var listResponse = await eventSinkClient.GetAsync("/api/s3", cancellationToken);

        var listContent = await listResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            listResponse.IsSuccessStatusCode,
            $"EventSink returned {(int)listResponse.StatusCode}: {listContent}");

        return JsonSerializer.Deserialize<List<string>>(listContent, _jsonOptions) ?? [];
    }

    /// <summary>
    /// Останавливает приложение и освобождает ресурсы тестовой среды.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}