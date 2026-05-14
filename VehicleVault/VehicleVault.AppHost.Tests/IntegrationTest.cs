using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VehicleVault.Api.Entities;
using Xunit.Abstractions;

namespace VehicleVault.AppHost.Tests;

/// <summary>
/// Интеграционные тесты пайплайна
/// </summary>
/// <param name="output">Журнал для xUnit</param>
public class IntegrationTest(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private DistributedApplication? _app;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var ct = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.VehicleVault_AppHost>(ct);
        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        _app = await builder.BuildAsync(ct);
        await _app.StartAsync(ct);
    }

    /// <summary>
    /// Дёргает эндпоинт API-Gateway с заданным id, ждёт обработки сообщения SQS-консьюмером
    /// и проверяет, что в S3-хранилище появился файл с этим же id
    /// </summary>
    [Fact]
    public async Task GatewayRequest_ShouldStoreVehicleFileInS3()
    {
        var ct = CancellationToken.None;
        var id = new Random().Next(1, 100);

        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/vehicle?id={id}", ct);
        Assert.Equal(HttpStatusCode.OK, gatewayResponse.StatusCode);

        var apiVehicle = JsonSerializer.Deserialize<Vehicle>(
            await gatewayResponse.Content.ReadAsStringAsync(ct), _jsonOptions);
        Assert.NotNull(apiVehicle);
        Assert.Equal(id, apiVehicle!.SystemId);

        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        using var fileClient = _app!.CreateHttpClient("file-service", "http");
        using var fileResponse = await fileClient.GetAsync($"/api/files/{id}", ct);
        Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);

        var s3Vehicle = JsonSerializer.Deserialize<Vehicle>(
            await fileResponse.Content.ReadAsStringAsync(ct), _jsonOptions);

        Assert.NotNull(s3Vehicle);
        Assert.Equal(id, s3Vehicle!.SystemId);
        Assert.Equivalent(apiVehicle, s3Vehicle);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_app is null) return;
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
