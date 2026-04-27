using System.Text.Json;
using Aspire.Hosting;
using CompanyEmployee.Generator.Dto;
using Microsoft.Extensions.Logging;
using Projects;
using Xunit.Abstractions;

namespace CompanyEmployee.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private DistributedApplication? _app;
    private HttpClient? _gatewayClient;
    private HttpClient? _s3Client;
    
    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<CompanyEmployee_AppHost>(cancellationToken);
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
        _s3Client = _app.CreateHttpClient("event-sink", "http");
    }
    
    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
    }
    
    /// <summary>
    /// Проверка положительного сценария:
    /// 1. Запрос возвращает сгенерированного сотрудника
    /// 2. Идентичный сотрудник сохраняется в S3
    /// </summary>
    [Fact]
    public async Task SuccessPipelineTest()
    {
        var id = 0;

        var response = await _gatewayClient!.GetAsync($"/company-employee?id={id}");
        var generatedEmployee =
            JsonSerializer.Deserialize<CompanyEmployeeDto>(await response.Content.ReadAsStringAsync(), _jsonOptions);

        var s3Employee = await GetEmployeeFromS3(id);

        Assert.NotNull(generatedEmployee);
        Assert.NotNull(s3Employee);
        Assert.Equivalent(generatedEmployee, s3Employee, strict: true);
    }
    
    /// <summary>
    /// Проверка обработки некорректных id
    /// </summary>
    /// <param name="id">id сотрудника</param>
    [Theory]
    [InlineData("-1")]
    [InlineData("qwe")]
    public async Task IncorrectEmployeeIdTest(string id)
    {
        var request = $"/company-employee?id={id}";

        var response = await _gatewayClient!.GetAsync(request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    /// <summary>
    /// Проверка кэширования - при запросах с одинаковым id возвращаются одинаковые сотрудники
    /// </summary>
    [Fact]
    public async Task CachingTest()
    {
        var id = 0;
        
        var response = await _gatewayClient!.GetAsync($"/company-employee?id={id}");
        var firstEmployee =
            JsonSerializer.Deserialize<CompanyEmployeeDto>(await response.Content.ReadAsStringAsync(), _jsonOptions);
        
        response = await _gatewayClient!.GetAsync($"/company-employee?id={id}");
        var secondEmployee =
            JsonSerializer.Deserialize<CompanyEmployeeDto>(await response.Content.ReadAsStringAsync(), _jsonOptions);
        
        Assert.NotNull(firstEmployee);
        Assert.NotNull(secondEmployee);
        Assert.Equivalent(firstEmployee, secondEmployee);
    }
    
    /// <summary>
    /// Получение сотрудника из S3
    /// </summary>
    /// <param name="id">id сотрудника</param>
    /// <returns>DTO сотрудника компании</returns>
    /// <exception cref="TimeoutException">Выбрасывается, если сотрудник не найден в S3</exception>
    private async Task<CompanyEmployeeDto> GetEmployeeFromS3(int id)
    {
        var endTime = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        var fileName = $"company_employee_{id}.json";

        while (DateTime.UtcNow < endTime)
        {
            var file = await _s3Client!.GetAsync($"/api/s3/{fileName}");
            if (file.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<CompanyEmployeeDto>(await file.Content.ReadAsStringAsync(),
                    _jsonOptions);
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"File with id {id} not found in S3");
    }
}