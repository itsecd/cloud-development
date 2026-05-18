using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using VehicleApp.Api.Models;
using Xunit.Abstractions;

namespace VehicleApp.AppHost.Tests;

/// <summary>
/// Интеграционные тесты
/// </summary>
/// <param name="output">xUnit-вывод</param>
public class IntegrationTest(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private DistributedApplication? _app;
    private HttpClient? _gatewayClient;
    private HttpClient? _fileServiceClient;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.VehicleApp_AppHost>(cancellationToken);
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

        _gatewayClient = _app.CreateHttpClient("api-gateway", "http");
        _fileServiceClient = _app.CreateHttpClient("file-service", "http");
    }

    /// <summary>
    /// Запрос через шлюз возвращает транспортное средство и сериализует его в файловое хранилище.
    /// Проверяется идентичность данных, полученных из API и из S3.
    /// </summary>
    [Fact]
    public async Task GatewayResponse_IsPersistedToObjectStorage()
    {
        var id = Random.Shared.Next(1, 100);

        using var gatewayResponse = await _gatewayClient!.GetAsync($"/vehicle?id={id}");
        gatewayResponse.EnsureSuccessStatusCode();
        var apiVehicle = JsonSerializer.Deserialize<Vehicle>(
            await gatewayResponse.Content.ReadAsStringAsync(), _serializerOptions);

        await Task.Delay(TimeSpan.FromSeconds(5));

        using var s3Response = await _fileServiceClient!.GetAsync($"/api/s3/vehicle_{id}.json");
        s3Response.EnsureSuccessStatusCode();
        var storedVehicle = JsonSerializer.Deserialize<Vehicle>(
            await s3Response.Content.ReadAsStringAsync(), _serializerOptions);

        Assert.NotNull(apiVehicle);
        Assert.NotNull(storedVehicle);
        Assert.Equal(id, storedVehicle!.Id);
        Assert.Equivalent(apiVehicle, storedVehicle);
    }

    /// <summary>
    /// После запроса транспортного средства в бакете присутствует соответствующий файл.
    /// </summary>
    [Fact]
    public async Task ObjectStorageList_ContainsGeneratedVehicle()
    {
        var id = Random.Shared.Next(101, 200);

        using var gatewayResponse = await _gatewayClient!.GetAsync($"/vehicle?id={id}");
        gatewayResponse.EnsureSuccessStatusCode();

        await Task.Delay(TimeSpan.FromSeconds(5));

        using var listResponse = await _fileServiceClient!.GetAsync("/api/s3");
        listResponse.EnsureSuccessStatusCode();
        var keys = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync(), _serializerOptions);

        Assert.NotNull(keys);
        Assert.Contains($"vehicle_{id}.json", keys!);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
