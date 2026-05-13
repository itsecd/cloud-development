using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Aspire.Hosting;
using Cloud.Api.Models;
using Microsoft.Extensions.Logging;
using Projects;
using System.Text.Json;

namespace Cloud.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна
/// </summary>
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
    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Cloud_AppHost>(cancellationToken);
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
    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// Проверка всего пайплайна: запрос через апи гейтвей, генерация сотрудника, сохранение в кэш, 
    /// отправка в SQS и сохранение файла в Minio
    /// </summary>
    [Fact]
    public async Task SuccessPipelineTest()
    {
        const int id = 42;

        var response = await _gatewayClient!.GetAsync($"/employee?id={id}", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var generatedEmployee = JsonSerializer.Deserialize<Employee>(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

        var s3Employee = await GetEmployeeFromS3(id);

        Assert.NotNull(generatedEmployee);
        Assert.NotNull(s3Employee);
        Assert.Equal(generatedEmployee!.Id, s3Employee!.Id);
        Assert.Equal(generatedEmployee.FullName, s3Employee.FullName);
    }

    /// <summary>
    /// Проверка того, что запросы с некорректным id возвращают 400 Bad Request
    /// </summary>
    /// <param name="id">id сотрудника</param>
    [Theory]
    [InlineData("-1")]
    [InlineData("qwe")]
    public async Task IncorrectEmployeeIdTest(string id)
    {
        var request = $"/employee?id={id}";
        var response = await _gatewayClient!.GetAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Проверка того, что повторный запрос с тем же id возвращает идентичного сотрудника из кэша
    /// </summary>
    [Fact]
    public async Task CachingTest()
    {
        const int id = 99;
        var firstResponse = await _gatewayClient!.GetAsync($"/employee?id={id}", TestContext.Current.CancellationToken);
        var firstEmployee = JsonSerializer.Deserialize<Employee>(
            await firstResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

        var secondResponse = await _gatewayClient!.GetAsync($"/employee?id={id}", TestContext.Current.CancellationToken);
        var secondEmployee = JsonSerializer.Deserialize<Employee>(
            await secondResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), _jsonOptions);

        Assert.NotNull(firstEmployee);
        Assert.NotNull(secondEmployee);
        Assert.Equal(firstEmployee!.Id, secondEmployee!.Id);
        Assert.Equal(firstEmployee.FullName, secondEmployee.FullName);

        var firstJson = JsonSerializer.Serialize(firstEmployee);
        var secondJson = JsonSerializer.Serialize(secondEmployee);
        Assert.Equal(firstJson, secondJson);
    }

    /// <summary>
    /// Проверка получения сотрудника из S3
    /// </summary>
    /// <param name="id">id сотрудника</param>
    /// <returns>Информация о сотруднике компании</returns>
    /// <exception cref="TimeoutException">Выбрасывается, если сотрудник не найден в S3</exception>
    private async Task<Employee?> GetEmployeeFromS3(int id)
    {
        var endTime = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        var fileName = $"cloud_employee_{id}.json";

        while (DateTime.UtcNow < endTime)
        {
            var fileResponse = await _s3Client!.GetAsync($"/api/s3/{fileName}", TestContext.Current.CancellationToken);
            if (fileResponse.IsSuccessStatusCode)
            {
                var content = await fileResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                return JsonSerializer.Deserialize<Employee>(content, _jsonOptions);
            }
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"File with id {id} not found in S3 within timeout.");
    }

    /// <summary>
    /// Проверка доступности всех реплик при массовых запросах через апи гейтвей
    /// </summary>
    [Fact]
    public async Task WeightedDistributionTest()
    {
        const int totalRequests = 200;
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var clients = new List<HttpClient>();

        for (var i = 0; i < 5; i++)
        {
            var client = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri($"https://localhost:{8000 + i}")
            };
            clients.Add(client);
        }

        var tasks = new List<Task<HttpResponseMessage>>();
        for (var i = 0; i < totalRequests; i++)
        {
            var id = Random.Shared.Next(1, 10000);
            tasks.Add(_gatewayClient!.GetAsync($"/employee?id={id}", TestContext.Current.CancellationToken));
        }

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        foreach (var client in clients)
        {
            var healthResponse = await client.GetAsync("/employee?id=1", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        }
    }

    /// <summary>
    /// Проверка устойчивости EventSink к некорректным сообщениям в очереди SQS
    /// </summary>
    [Fact]
    public async Task DeadLetterQueueTest()
    {
        var badJson = """{"invalid": "data", "missing": "Id field"}""";

        var queueUrl = Environment.GetEnvironmentVariable("AWS__Resources__SQSQueueUrl")
                       ?? "http://sqs.eu-central-1.localhost:4566/000000000000/employee-queue";

        var sqsClient = new AmazonSQSClient(
            new BasicAWSCredentials("dummy", "dummy"),
            new AmazonSQSConfig { ServiceURL = "http://localhost:4566" });

        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = badJson
        });

        await Task.Delay(5000, TestContext.Current.CancellationToken);

        var healthResponse = await _s3Client!.GetAsync("/api/s3", TestContext.Current.CancellationToken);
        Assert.True(healthResponse.IsSuccessStatusCode, "EventSink should still be running");

        var filesResponse = await _s3Client.GetAsync("/api/s3", TestContext.Current.CancellationToken);
        var files = JsonSerializer.Deserialize<List<string>>(
            await filesResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(files);
        Assert.DoesNotContain(files, f => f.Contains("invalid"));

        var id = Random.Shared.Next(50000, 60000);
        var response = await _gatewayClient!.GetAsync($"/employee?id={id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var s3Employee = await GetEmployeeFromS3(id);
        Assert.NotNull(s3Employee);
        Assert.Equal(id, s3Employee!.Id);
    }
}