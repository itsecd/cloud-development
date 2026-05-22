using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Patient.Generator.DTO;
using System.Text.Json;
using Xunit.Abstractions;

namespace Patient.AppHost.Tests;

/// <summary>
/// Интеграционные тесты микросервисного пайплайна (SQS + Minio)
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов.</param>
public sealed class IntegrationTest(ITestOutputHelper output) : IAsyncLifetime
{
    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Patient_AppHost>(cancellationToken);
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
    }

    /// <summary>
    /// Проверяет весь пайплайн: запрос к гейтвею генерирует пациента, тот через SQS
    /// попадает в Minio, и сохранённый файл совпадает с ответом API
    /// </summary>
    [Fact]
    public async Task TestPipeline()
    {
        var cancellationToken = CancellationToken.None;

        var random = new Random();
        var id = random.Next(1, 1000);

        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/api/patient?id={id}", cancellationToken);
        var apiPatient = JsonSerializer.Deserialize<PatientDto>(
            await gatewayResponse.Content.ReadAsStringAsync(cancellationToken));

        await Task.Delay(5000, cancellationToken);

        using var fileClient = _app!.CreateHttpClient("file-service", "http");
        using var listResponse = await fileClient.GetAsync("/api/s3", cancellationToken);
        var fileList = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync(cancellationToken));

        using var s3Response = await fileClient.GetAsync($"/api/s3/patient_{id}.json", cancellationToken);
        var s3Patient = JsonSerializer.Deserialize<PatientDto>(
            await s3Response.Content.ReadAsStringAsync(cancellationToken));

        Assert.NotNull(fileList);
        Assert.Contains($"patient_{id}.json", fileList);
        Assert.NotNull(apiPatient);
        Assert.NotNull(s3Patient);
        Assert.Equal(id, s3Patient.Id);
        Assert.Equivalent(apiPatient, s3Patient);
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
