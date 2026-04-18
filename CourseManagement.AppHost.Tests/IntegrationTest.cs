using Aspire.Hosting;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using CourseManagement.ApiService.Entities;

namespace CourseManagement.AppHost.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
/// <param name="output">Служба журналирования unit-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CourseManagement_AppHost>(cancellationToken);
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
    /// Тестирование корректности работы сервиса генерации, api-gateway, сервисов взаимодействия с S3 и SNS 
    /// </summary>
    [Fact]
    public async Task TestAppServices()
    {
        Assert.NotNull(_builder);
        _app = await _builder.BuildAsync();
        await _app.StartAsync();

        await Task.Delay(10000);

        var id = new Random().Next(1, 100);

        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };

        using var gatewayClient = _app.CreateHttpClient("course-gateway", "http");

        using var gatewayResponse = await gatewayClient.GetAsync($"/course-management?id={id}");

        var content = await gatewayResponse.Content.ReadAsStringAsync();

        Assert.True(gatewayResponse.IsSuccessStatusCode, $"Gateway failed: {gatewayResponse.StatusCode} - {content}");

        var apiCourse = JsonSerializer.Deserialize<Course>(content);
        Assert.NotNull(apiCourse);

        await Task.Delay(5000);

        using var storageClient = _app.CreateHttpClient("course-storage", "http");

        using var listResponse = await storageClient.GetAsync("/api/s3");
        var listContent = await listResponse.Content.ReadAsStringAsync();
   
        var courseList = JsonSerializer.Deserialize<List<string>>(listContent);
        Assert.NotNull(courseList);
        Assert.NotEmpty(courseList);

        var matchingFile = courseList.FirstOrDefault(f => f.Contains($"course_{id}"));
        Assert.NotNull(matchingFile);

        using var s3Response = await storageClient.GetAsync($"/api/s3/{matchingFile}");
        var s3Content = await s3Response.Content.ReadAsStringAsync();
        var s3Course = JsonSerializer.Deserialize<Course>(s3Content);

        Assert.NotNull(s3Course);
        Assert.Equal(id, s3Course.Id);
        Assert.Equivalent(apiCourse, s3Course);
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        await _app!.StopAsync();
        await _app.DisposeAsync();
        await _builder!.DisposeAsync();
    }
}