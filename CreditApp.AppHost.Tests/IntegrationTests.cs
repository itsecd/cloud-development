using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using Xunit.Abstractions;

namespace CreditApp.AppHost.Tests;

public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        _builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CreditApp_AppHost>(CancellationToken.None);
        _builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        _builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        _app = await _builder.BuildAsync(CancellationToken.None);
        await _app.StartAsync(CancellationToken.None);
    }

    private HttpClient CreateGatewayClient() => _app!.CreateHttpClient("api-gateway", "http");
    private HttpClient CreateFileStorageClient() => _app!.CreateHttpClient("service-filestorage", "http");

    private static int GetId(JsonNode node) =>
        (int)(node["id"] ?? node["Id"])!;

    /// <summary>
    /// Ожидает появления файла в S3 через API файлового сервиса
    /// </summary>
    private static async Task<string?> WaitForFileAsync(HttpClient client, string fileName, int maxAttempts = 10)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(2000);
            using var response = await client.GetAsync("/api/s3");
            if (!response.IsSuccessStatusCode) continue;

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonNode.Parse(json)?.AsArray();
            if (list != null && list.Any(item => item?.ToString() == fileName))
                return fileName;
        }
        return null;
    }

    /// <summary>
    /// Проверяет, что API Gateway возвращает 200 OK при запросе кредитной заявки
    /// </summary>
    [Fact]
    public async Task GatewayReturnsOkForCreditApplication()
    {
        using var client = CreateGatewayClient();
        using var response = await client.GetAsync("/credit-application?id=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет, что API возвращает корректную кредитную заявку с правильным Id
    /// </summary>
    [Fact]
    public async Task ApiReturnsCreditApplicationWithCorrectId()
    {
        var id = 42;
        using var client = CreateGatewayClient();
        using var response = await client.GetAsync($"/credit-application?id={id}");
        var json = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);

        Assert.NotNull(node);
        Assert.Equal(id, GetId(node));
    }

    /// <summary>
    /// Проверяет, что повторный запрос возвращает закэшированные данные (идентичный результат)
    /// </summary>
    [Fact]
    public async Task RepeatedRequestReturnsCachedData()
    {
        var id = 55;
        using var client = CreateGatewayClient();

        using var response1 = await client.GetAsync($"/credit-application?id={id}");
        var json1 = await response1.Content.ReadAsStringAsync();

        using var response2 = await client.GetAsync($"/credit-application?id={id}");
        var json2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(json1, json2);
    }

    /// <summary>
    /// Проверяет полный пайплайн: генерация → SNS → FileStorage → Minio.
    /// Файл должен появиться в списке S3.
    /// </summary>
    [Fact]
    public async Task PipelineSavesFileToMinio()
    {
        var id = new Random().Next(1, 100);

        using var gatewayClient = CreateGatewayClient();
        using var gatewayResponse = await gatewayClient.GetAsync($"/credit-application?id={id}");
        Assert.Equal(HttpStatusCode.OK, gatewayResponse.StatusCode);

        using var sinkClient = CreateFileStorageClient();
        var found = await WaitForFileAsync(sinkClient, $"creditapp_{id}.json");

        Assert.NotNull(found);
    }

    /// <summary>
    /// Проверяет, что данные из API и из Minio идентичны
    /// </summary>
    [Fact]
    public async Task StoredDataMatchesApiResponse()
    {
        var id = new Random().Next(100, 200);

        using var gatewayClient = CreateGatewayClient();
        using var gatewayResponse = await gatewayClient.GetAsync($"/credit-application?id={id}");
        var apiJson = await gatewayResponse.Content.ReadAsStringAsync();
        var apiNode = JsonNode.Parse(apiJson);
        Assert.NotNull(apiNode);

        using var sinkClient = CreateFileStorageClient();
        var found = await WaitForFileAsync(sinkClient, $"creditapp_{id}.json");
        Assert.NotNull(found);

        using var s3Response = await sinkClient.GetAsync($"/api/s3/creditapp_{id}.json");
        Assert.Equal(HttpStatusCode.OK, s3Response.StatusCode);
        var s3Json = await s3Response.Content.ReadAsStringAsync();
        var s3Node = JsonNode.Parse(s3Json);
        Assert.NotNull(s3Node);

        Assert.Equal(id, GetId(s3Node));
        Assert.Equal(GetId(apiNode), GetId(s3Node));
    }

    /// <summary>
    /// Проверяет, что FileStorage health endpoint отвечает 200 OK
    /// </summary>
    [Fact]
    public async Task FileStorageHealthEndpointReturnsOk()
    {
        using var client = CreateFileStorageClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        if (_builder is not null)
            await _builder.DisposeAsync();
    }
}
