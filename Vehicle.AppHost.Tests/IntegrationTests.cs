using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Vehicle.Api.Entities;
using Xunit.Abstractions;

namespace Vehicle.AppHost.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна Vehicle.
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов.</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    private static readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;

        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Vehicle_AppHost>(cancellationToken);

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
    /// <item><description>В ответ отправляет сгенерированное транспортное средство.</description></item>
    /// <item><description>Отправляет данные через SNS в Vehicle.EventSink.</description></item>
    /// <item><description>Сериализует транспортное средство в JSON-файл и сохраняет его в Minio.</description></item>
    /// <item><description>Проверяет, что данные из API и объектного хранилища идентичны.</description></item>
    /// </list>
    /// </summary>
    /// <param name="envName">Запускаемый профиль окружения.</param>
    [Theory]
    [InlineData("SNS+MinioS3")]
    public async Task TestPipeline(string envName)
    {
        var cancellationToken = CancellationToken.None;

        _builder!.Environment.EnvironmentName = envName;

        _app = await _builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        using var sinkClient = _app.CreateHttpClient(
            "vehicle-event-sink",
            "event-sink-http");

        // Ждем, пока Vehicle.EventSink и Minio начнут отвечать.
        await WaitForEventSinkAsync(
            sinkClient,
            TimeSpan.FromSeconds(60),
            cancellationToken);

        // Даем SNS subscription время подтвердиться после старта EventSink.
        await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);

        var id = Random.Shared.Next(100_000, 999_999);

        using var gatewayClient = _app.CreateHttpClient(
            "vehicle-gateway",
            "vehicle-gateway-lb");

        using var gatewayResponse = await gatewayClient.GetAsync(
            $"/gateway/Vehicles?id={id}",
            cancellationToken);

        var gatewayContent = await gatewayResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            gatewayResponse.IsSuccessStatusCode,
            $"Gateway returned {(int)gatewayResponse.StatusCode}: {gatewayContent}");

        var apiVehicle = JsonSerializer.Deserialize<VehicleEntity>(
            gatewayContent,
            _jsonOptions);

        Assert.NotNull(apiVehicle);
        Assert.Equal(id, apiVehicle.Id);

        var vehicleFileName = await WaitForVehicleFileAsync(
            sinkClient,
            id,
            TimeSpan.FromSeconds(90),

        output, cancellationToken);

        Assert.False(
            string.IsNullOrWhiteSpace(vehicleFileName),
            $"File vehicle_{id}_*.json was not found in Minio");

        using var s3Response = await sinkClient.GetAsync(
            $"/api/s3/{vehicleFileName}",
            cancellationToken);

        var s3Content = await s3Response.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(
            s3Response.IsSuccessStatusCode,
            $"EventSink returned {(int)s3Response.StatusCode}: {s3Content}");

        var s3Vehicle = JsonSerializer.Deserialize<VehicleEntity>(
            s3Content,
            _jsonOptions);

        Assert.NotNull(s3Vehicle);
        Assert.Equal(id, s3Vehicle.Id);
        Assert.Equivalent(apiVehicle, s3Vehicle);
    }

    /// <summary>
    /// Ждет, пока Vehicle.EventSink начнет отвечать на запросы к /api/s3.
    /// </summary>
    private static async Task WaitForEventSinkAsync(
        HttpClient sinkClient,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var lastResponse = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await sinkClient.GetAsync(
                    "/api/s3",
                    cancellationToken);

                lastResponse = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastResponse = ex.Message;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        throw new TimeoutException(
            $"Vehicle.EventSink did not become ready in time. Last response: {lastResponse}");
    }

    /// <summary>
    /// Ждет появления JSON-файла vehicle_{id}_*.json в Minio.
    /// </summary>
    private static async Task<string?> WaitForVehicleFileAsync(
        HttpClient sinkClient,
        int vehicleId,
        TimeSpan timeout,
        ITestOutputHelper output, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var expectedPrefix = $"vehicle_{vehicleId}_";
        var lastFileList = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            using var listResponse = await sinkClient.GetAsync(
                "/api/s3",
                cancellationToken);

            lastFileList = await listResponse.Content.ReadAsStringAsync(cancellationToken);

            if (listResponse.IsSuccessStatusCode)
            {
                var vehicleList = JsonSerializer.Deserialize<List<string>>(
                    lastFileList,
                    _jsonOptions);

                var vehicleFileName = vehicleList?
                    .FirstOrDefault(file => file.StartsWith(
                        expectedPrefix,
                        StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(vehicleFileName))
                {
                    return vehicleFileName;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        output.WriteLine(
            $"File with prefix {expectedPrefix} was not found. Last file list: {lastFileList}");

        return null;
    }

    /// <inheritdoc/>
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