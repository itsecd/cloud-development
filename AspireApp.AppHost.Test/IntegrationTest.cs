using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using Service.Api.Entities;
using System.Text.Json;
using Xunit.Abstractions;

namespace AspireApp.AppHost.Test;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireApp_AppHost>(cancellationToken);
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
    /// Проверяет, что вызов гейтвея:
    /// <list type="bullet">
    /// <item><description>В ответ отправляет сгенерированного сотрудника</description></item>
    /// <item><description>Сериализует сотрудника в S3 хранилище</description></item>
    /// <item><description>Проверяет, что данные из предыдущих пунктов идентичны</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task TestPipeline()
    {
        var random = new Random();
        var id = random.Next(1, 100);
        using var gatewayClient = _app.CreateHttpClient("employee-api-gateway", "http");
        using var gatewayResponse = await gatewayClient!.GetAsync($"/employee?id={id}");
        var apiEmployee = JsonSerializer.Deserialize<Employee>(await gatewayResponse.Content.ReadAsStringAsync());

        await Task.Delay(5000);
        using var sinkClient = _app.CreateHttpClient("employee-sink", "http");
        using var listResponse = await sinkClient!.GetAsync($"/api/s3");
        var employeeList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());
        using var s3Response = await sinkClient!.GetAsync($"/api/s3/employee_{id}.json");
        var s3Employee = JsonSerializer.Deserialize<Employee>(await s3Response.Content.ReadAsStringAsync());

        Assert.NotNull(employeeList);
        Assert.Single(employeeList);
        Assert.NotNull(apiEmployee);
        Assert.NotNull(s3Employee);
        Assert.Equal(id, s3Employee.Id);
        Assert.Equivalent(apiEmployee, s3Employee);
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