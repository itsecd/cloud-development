using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using SoftwareProjects.Api.Entities;
using System.Text.Json;
using Xunit.Abstractions;

namespace SoftwareProjects.AppHost.Tests;

/// <summary>
/// Интеграционные тесты, проверяющие совместную работу
/// API-сервиса, кэша Redis, брокера SNS и файлового сервиса с S3-хранилищем (LocalStack)
/// </summary>
/// <param name="output">Журнал юнит-тестов; пробрасывается в логирование Aspire-хоста</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    /// <summary>
    /// Время, которое тесты ждут после HTTP-запроса к API,
    /// чтобы сообщение успело пройти SNS → File.Service → S3
    /// </summary>
    private static readonly TimeSpan _propagationDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Опции десериализации, совпадающие с тем, как ASP.NET сериализует ответы Minimal API
    /// (camelCase свойства)
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private DistributedApplication? _app;
    private HttpClient? _gatewayClient;
    private HttpClient? _fileServiceClient;


    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SoftwareProjects_AppHost>(cancellationToken);
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
        _gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        _fileServiceClient = _app!.CreateHttpClient("file-service", "http");
    }

    /// <summary>
    /// Сценарий «End-to-end»: запрос на гейтвей → API генерирует и публикует проект в SNS →
    /// File.Service сохраняет JSON в S3 → проверяем, что файл по ключу совпадает с ответом API
    /// </summary>
    [Fact]
    public async Task Pipeline_GatewayRequest_PersistsGeneratedProjectToS3()
    {
        var id = Random.Shared.Next(1_000, 9_999);

        using var gatewayResponse = await _gatewayClient!.GetAsync($"/software-projects?id={id}");
        gatewayResponse.EnsureSuccessStatusCode();
        var apiProject = JsonSerializer.Deserialize<SoftwareProject>(
            await gatewayResponse.Content.ReadAsStringAsync(), _jsonOptions);

        await Task.Delay(_propagationDelay);

        using var s3Response = await _fileServiceClient!.GetAsync($"/api/s3/software-project-{id}.json");
        s3Response.EnsureSuccessStatusCode();
        var s3Project = JsonSerializer.Deserialize<SoftwareProject>(
            await s3Response.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(apiProject);
        Assert.NotNull(s3Project);
        Assert.Equal(id, s3Project.Id);
        Assert.Equivalent(apiProject, s3Project);
    }

    /// <summary>
    /// Сценарий со списком: после нескольких запросов с разными id в бакете
    /// должно появиться столько же файлов с ожидаемыми ключами
    /// </summary>
    [Fact]
    public async Task Pipeline_MultipleRequests_AreAllListedInBucket()
    {
        var ids = new[]
        {
            Random.Shared.Next(10_000, 19_999),
            Random.Shared.Next(20_000, 29_999),
            Random.Shared.Next(30_000, 39_999)
        };

        foreach (var id in ids)
        {
            using var response = await _gatewayClient!.GetAsync($"/software-projects?id={id}");
            response.EnsureSuccessStatusCode();
        }

        await Task.Delay(_propagationDelay);

        using var listResponse = await _fileServiceClient!.GetAsync("/api/s3");
        listResponse.EnsureSuccessStatusCode();
        var keys = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(keys);
        foreach (var id in ids)
            Assert.Contains($"software-project-{id}.json", keys);
    }

    /// <summary>
    /// Сценарий с кэшем: повторный запрос того же id должен попасть в Redis-кэш и
    /// не приводить к повторной публикации в SNS, поэтому в S3 остаётся ровно одна версия файла
    /// </summary>
    [Fact]
    public async Task Pipeline_RepeatedRequests_DoNotProduceDuplicateFiles()
    {
        var id = Random.Shared.Next(40_000, 49_999);

        using var firstResponse = await _gatewayClient!.GetAsync($"/software-projects?id={id}");
        firstResponse.EnsureSuccessStatusCode();
        var firstPayload = await firstResponse.Content.ReadAsStringAsync();
        var firstProject = JsonSerializer.Deserialize<SoftwareProject>(firstPayload, _jsonOptions);

        await Task.Delay(_propagationDelay);

        for (var i = 0; i < 3; i++)
        {
            using var repeated = await _gatewayClient!.GetAsync($"/software-projects?id={id}");
            repeated.EnsureSuccessStatusCode();
            var repeatedProject = JsonSerializer.Deserialize<SoftwareProject>(
                await repeated.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(repeatedProject);
            Assert.Equivalent(firstProject, repeatedProject);
        }

        await Task.Delay(_propagationDelay);

        using var listResponse = await _fileServiceClient!.GetAsync("/api/s3");
        listResponse.EnsureSuccessStatusCode();
        var keys = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(keys);
        var duplicates = keys.Count(k => k == $"software-project-{id}.json");
        Assert.Equal(1, duplicates);
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
