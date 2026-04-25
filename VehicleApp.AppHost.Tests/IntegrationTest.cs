using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using VehicleApp.Api.Models;
using Xunit.Abstractions;

namespace VehicleApp.AppHost.Tests;

/// <summary>
/// Интеграционные тесты пайплайна Gateway → Api → SNS → File.Service → Minio
/// </summary>
/// <param name="output">Логгер xUnit для вывода в консоль теста</param>
public class IntegrationTest(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.VehicleApp_AppHost>(cancellationToken);
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
    }

    /// <summary>
    /// Основной сценарий: запрос по id через гейтвей, затем проверка, что в Minio
    /// лежит файл <c>vehicle_{id}.json</c> с теми же данными, что вернул API
    /// </summary>
    [Fact]
    public async Task TestPipeline()
    {
        var random = new Random();
        var id = random.Next(1, 10_000);

        Assert.NotNull(_app);
        using var gatewayClient = _app.CreateHttpClient("vehicleapp-gateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/vehicles?id={id}");
        var apiVehicle = JsonSerializer.Deserialize<Vehicle>(await gatewayResponse.Content.ReadAsStringAsync(), _json);

        await Task.Delay(5000);
        using var fileClient = _app.CreateHttpClient("file-service", "http");
        using var listResponse = await fileClient.GetAsync("/api/s3");
        var vehicleList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync(), _json);
        using var s3Response = await fileClient.GetAsync($"/api/s3/vehicle_{id}.json");
        var s3Vehicle = JsonSerializer.Deserialize<Vehicle>(await s3Response.Content.ReadAsStringAsync(), _json);

        Assert.NotNull(vehicleList);
        Assert.Single(vehicleList);
        Assert.NotNull(apiVehicle);
        Assert.NotNull(s3Vehicle);
        Assert.Equal(id, s3Vehicle.Id);
        Assert.Equivalent(apiVehicle, s3Vehicle);
    }

    /// <summary>
    /// Проверяет, что файл в Minio сохраняется под ключом <c>vehicle_{id}.json</c>
    /// </summary>
    [Fact]
    public async Task StoredKey_MatchesRequestedId()
    {
        var id = new Random().Next(1, 10_000);

        Assert.NotNull(_app);
        using var gatewayClient = _app.CreateHttpClient("vehicleapp-gateway", "http");
        using var _ = await gatewayClient.GetAsync($"/vehicles?id={id}");

        await Task.Delay(5000);
        using var fileClient = _app.CreateHttpClient("file-service", "http");
        using var listResponse = await fileClient.GetAsync("/api/s3");
        var list = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync(), _json);

        Assert.NotNull(list);
        Assert.Contains($"vehicle_{id}.json", list);
    }

    /// <summary>
    /// Запрос несуществующего ключа должен возвращать 404
    /// </summary>
    [Fact]
    public async Task GetFile_ReturnsNotFound_WhenKeyDoesNotExist()
    {
        Assert.NotNull(_app);
        using var fileClient = _app.CreateHttpClient("file-service", "http");
        using var response = await fileClient.GetAsync("/api/s3/vehicle_999999.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Повторный запрос с тем же id отдаётся из кэша и не приводит к появлению
    /// второго файла в Minio
    /// </summary>
    [Fact]
    public async Task RepeatedRequest_DoesNotDuplicateFileInMinio()
    {
        var id = new Random().Next(1, 10_000);

        Assert.NotNull(_app);
        using var gatewayClient = _app.CreateHttpClient("vehicleapp-gateway", "http");
        using var first = await gatewayClient.GetAsync($"/vehicles?id={id}");
        var firstVehicle = JsonSerializer.Deserialize<Vehicle>(await first.Content.ReadAsStringAsync(), _json);

        await Task.Delay(5000);

        using var second = await gatewayClient.GetAsync($"/vehicles?id={id}");
        var secondVehicle = JsonSerializer.Deserialize<Vehicle>(await second.Content.ReadAsStringAsync(), _json);

        await Task.Delay(3000);

        using var fileClient = _app.CreateHttpClient("file-service", "http");
        using var listResponse = await fileClient.GetAsync("/api/s3");
        var list = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync(), _json);

        Assert.NotNull(firstVehicle);
        Assert.NotNull(secondVehicle);
        Assert.Equivalent(firstVehicle, secondVehicle);
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal($"vehicle_{id}.json", list[0]);
    }

    /// <summary>
    /// Содержимое файла в Minio совпадает с ответом API по всем полям ТС
    /// </summary>
    [Fact]
    public async Task StoredVehicle_HasSameFieldsAsApiResponse()
    {
        var id = new Random().Next(1, 10_000);

        Assert.NotNull(_app);
        using var gatewayClient = _app.CreateHttpClient("vehicleapp-gateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/vehicles?id={id}");
        var apiVehicle = JsonSerializer.Deserialize<Vehicle>(await gatewayResponse.Content.ReadAsStringAsync(), _json);

        await Task.Delay(5000);
        using var fileClient = _app.CreateHttpClient("file-service", "http");
        using var s3Response = await fileClient.GetAsync($"/api/s3/vehicle_{id}.json");
        var s3Vehicle = JsonSerializer.Deserialize<Vehicle>(await s3Response.Content.ReadAsStringAsync(), _json);

        Assert.NotNull(apiVehicle);
        Assert.NotNull(s3Vehicle);
        Assert.Equal(apiVehicle.Id, s3Vehicle.Id);
        Assert.Equal(apiVehicle.Vin, s3Vehicle.Vin);
        Assert.Equal(apiVehicle.Manufacturer, s3Vehicle.Manufacturer);
        Assert.Equal(apiVehicle.Model, s3Vehicle.Model);
        Assert.Equal(apiVehicle.Year, s3Vehicle.Year);
        Assert.Equal(apiVehicle.BodyType, s3Vehicle.BodyType);
        Assert.Equal(apiVehicle.FuelType, s3Vehicle.FuelType);
        Assert.Equal(apiVehicle.BodyColor, s3Vehicle.BodyColor);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
        await _builder!.DisposeAsync();
    }
}
