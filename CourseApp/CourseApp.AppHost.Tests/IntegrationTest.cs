using Aspire.Hosting;
using CourseApp.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit.Abstractions;

namespace CourseApp.AppHost.Tests;

/// <summary>
/// Интеграционные тесты пайплайна
/// </summary>
public sealed class IntegrationTest(ITestOutputHelper output) : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _gatewayClient;
    private HttpClient? _sinkClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CourseApp_AppHost>(cancellationToken);
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
        _sinkClient = _app.CreateHttpClient("courseapp-fileservice", "http");
    }

    /// <summary>
    /// Главный happy-path: запрос к API кладёт сериализованный курс в S3 идентичным тому, что вернулся клиенту
    /// </summary>
    [Fact]
    public async Task Pipeline_PutsGeneratedCourseToS3()
    {
        var random = new Random();
        var id = random.Next(1, 500);
        using var gatewayResponse = await _gatewayClient!.GetAsync($"/courses?id={id}");
        Assert.True(gatewayResponse.IsSuccessStatusCode);

        var apiCourse = JsonSerializer.Deserialize<Course>(
            await gatewayResponse.Content.ReadAsStringAsync(), _jsonOptions);

        await Task.Delay(5000);

        using var s3Response = await _sinkClient!.GetAsync($"/api/s3/course_{id}.json");
        Assert.True(s3Response.IsSuccessStatusCode);

        var s3Course = JsonSerializer.Deserialize<Course>(
            await s3Response.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(apiCourse);
        Assert.NotNull(s3Course);
        Assert.Equal(id, s3Course!.Id);
        Assert.Equivalent(apiCourse, s3Course);
    }

    /// <summary>
    /// Идемпотентность: cache hit не публикует в SQS повторно.
    /// Дважды зовём один и тот же id; в S3 должен лежать ровно один файл course_{id}.json
    /// </summary>
    [Fact]
    public async Task Pipeline_CacheHitDoesNotDuplicateFile()
    {
        var random = new Random();
        var id = random.Next(1, 500);
        using var firstResponse = await _gatewayClient!.GetAsync($"/courses?id={id}");
        Assert.True(firstResponse.IsSuccessStatusCode);

        using var secondResponse = await _gatewayClient!.GetAsync($"/courses?id={id}");
        Assert.True(secondResponse.IsSuccessStatusCode);

        await Task.Delay(5000);

        using var listResponse = await _sinkClient!.GetAsync("/api/s3");
        Assert.True(listResponse.IsSuccessStatusCode);

        var keys = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(keys);
        Assert.Single(keys!, key => key == $"course_{id}.json");
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
