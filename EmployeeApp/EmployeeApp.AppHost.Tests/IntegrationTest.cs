using Aspire.Hosting;
using EmployeeApp.Api.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit.Abstractions;

namespace EmployeeApp.AppHost.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.EmployeeApp_AppHost>(cancellationToken);
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
    /// <item><description>В ответ отправляет сгенерированного сотрудника</description></item>
    /// <item><description>Сериализует сотрудника в S3 хранилище</description></item>
    /// <item><description>Проверяет, что данные из предыдущих пунктов идентичны</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task TestPipeline()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var id = new Random().Next(1, 100);

        using var gatewayClient = _app.CreateHttpClient("api-gateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/employees?id={id}");
        var apiEmployee = JsonSerializer.Deserialize<Employee>(await gatewayResponse.Content.ReadAsStringAsync(), _jsonOptions);

        await Task.Delay(5000);

        using var sinkClient = _app.CreateHttpClient("file-service", "http");
        using var listResponse = await sinkClient.GetAsync("/api/s3");
        var employeeList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());
        using var s3Response = await sinkClient.GetAsync($"/api/s3/employee_{id}.json");
        var s3Employee = JsonSerializer.Deserialize<Employee>(await s3Response.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(employeeList);
        Assert.Single(employeeList);
        Assert.Equal($"employee_{id}.json", employeeList[0]);
        Assert.NotNull(apiEmployee);
        Assert.NotNull(s3Employee);
        Assert.Equal(id, s3Employee.Id);
        Assert.Equivalent(apiEmployee, s3Employee);
    }

    /// <summary>
    /// Проверяет, что повторный вызов гейтвея для одного и того же идентификатора:
    /// <list type="bullet">
    /// <item><description>Возвращает идентичного сотрудника (сработал Redis-кэш)</description></item>
    /// <item><description>Не создает дубликат файла в S3 (сообщение в SNS отправляется только при промахе кэша)</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task TestCacheHitDoesNotDuplicateS3File()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var id = new Random().Next(200, 300);

        using var gatewayClient = _app.CreateHttpClient("api-gateway", "http");
        using var firstResponse = await gatewayClient.GetAsync($"/employees?id={id}");
        var firstEmployee = JsonSerializer.Deserialize<Employee>(await firstResponse.Content.ReadAsStringAsync(), _jsonOptions);
        using var secondResponse = await gatewayClient.GetAsync($"/employees?id={id}");
        var secondEmployee = JsonSerializer.Deserialize<Employee>(await secondResponse.Content.ReadAsStringAsync(), _jsonOptions);

        await Task.Delay(5000);

        using var sinkClient = _app.CreateHttpClient("file-service", "http");
        using var listResponse = await sinkClient.GetAsync("/api/s3");
        var employeeList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(firstEmployee);
        Assert.NotNull(secondEmployee);
        Assert.Equivalent(firstEmployee, secondEmployee);
        Assert.NotNull(employeeList);
        Assert.Single(employeeList);
        Assert.Equal($"employee_{id}.json", employeeList[0]);
    }

    /// <summary>
    /// Проверяет, что разные идентификаторы порождают независимые объекты в S3:
    /// <list type="bullet">
    /// <item><description>Для каждого идентификатора создается отдельный файл employee_{id}.json</description></item>
    /// <item><description>Содержимое каждого файла соответствует ответу API для этого идентификатора</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task TestMultipleIdsProduceDistinctS3Objects()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var ids = Enumerable.Range(0, 3).Select(_ => new Random().Next(400, 500)).Distinct().ToArray();

        using var gatewayClient = _app.CreateHttpClient("api-gateway", "http");
        var apiEmployees = new Dictionary<int, Employee?>();
        foreach (var id in ids)
        {
            using var response = await gatewayClient.GetAsync($"/employees?id={id}");
            apiEmployees[id] = JsonSerializer.Deserialize<Employee>(await response.Content.ReadAsStringAsync(), _jsonOptions);
        }

        await Task.Delay(5000);

        using var sinkClient = _app.CreateHttpClient("file-service", "http");
        using var listResponse = await sinkClient.GetAsync("/api/s3");
        var employeeList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(employeeList);
        Assert.Equal(ids.Length, employeeList.Count);

        foreach (var id in ids)
        {
            using var s3Response = await sinkClient.GetAsync($"/api/s3/employee_{id}.json");
            var s3Employee = JsonSerializer.Deserialize<Employee>(await s3Response.Content.ReadAsStringAsync(), _jsonOptions);

            Assert.Contains($"employee_{id}.json", employeeList);
            Assert.NotNull(s3Employee);
            Assert.Equal(id, s3Employee.Id);
            Assert.Equivalent(apiEmployees[id], s3Employee);
        }
    }

    /// <summary>
    /// Проверяет, что запрос несуществующего ключа в S3 возвращает некорректный статус, а не падает
    /// </summary>
    [Fact]
    public async Task TestMissingS3KeyReturnsError()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        using var sinkClient = _app.CreateHttpClient("file-service", "http");
        using var response = await sinkClient.GetAsync("/api/s3/employee_nonexistent.json");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет корректность балансировки: запросы с разными идентификаторами
    /// маршрутизируются через гейтвей к нужной реплике и обрабатываются успешно
    /// </summary>
    [Fact]
    public async Task TestGatewayRoutesRequestForEveryReplica()
    {
        var cancellationToken = CancellationToken.None;
        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        using var gatewayClient = _app.CreateHttpClient("api-gateway", "http");
        foreach (var id in Enumerable.Range(1, 5))
        {
            using var response = await gatewayClient.GetAsync($"/employees?id={id}");
            var employee = JsonSerializer.Deserialize<Employee>(await response.Content.ReadAsStringAsync(), _jsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(employee);
            Assert.Equal(id, employee.Id);
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
        await _builder!.DisposeAsync();
    }
}
