using System.Net;
using System.Text.Json;
using Aspire.Hosting;

namespace Backend.IntegrationTests;

public sealed class EmployeeExportIntegrationTests : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromMinutes(2);

    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireApp_AppHost>();
        _app = await builder.BuildAsync().WaitAsync(StartupTimeout);
        await _app.StartAsync().WaitAsync(StartupTimeout);

        var fileClient = _app.CreateHttpClient("file-service");
        var apiClient = _app.CreateHttpClient("service-api-0");

        using var cts = new CancellationTokenSource(StartupTimeout);

        await WaitForFileServiceReadyAsync(fileClient, cts.Token);
        await WaitForApiAsync(apiClient, cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task GeneratedEmployeeIsEventuallyExportedToObjectStorage()
    {
        Assert.NotNull(_app);

        const int employeeId = 501;

        var apiClient = _app!.CreateHttpClient("service-api-0");
        var fileClient = _app.CreateHttpClient("file-service");

        using var response = await apiClient.GetAsync($"/employee?id={employeeId}");
        response.EnsureSuccessStatusCode();

        var generatedJson = await response.Content.ReadAsStringAsync();
        var exportedJson = await WaitForExportAsync(fileClient, employeeId);

        Assert.Equal(Normalize(generatedJson), Normalize(exportedJson));
    }

    [Fact]
    public async Task SameEmployeeIdReturnsCachedPayloadAndExportRemainsAvailable()
    {
        Assert.NotNull(_app);

        const int employeeId = 777;

        var apiClient = _app!.CreateHttpClient("service-api-0");
        var fileClient = _app.CreateHttpClient("file-service");

        var first = await apiClient.GetStringAsync($"/employee?id={employeeId}");
        var second = await apiClient.GetStringAsync($"/employee?id={employeeId}");

        Assert.Equal(Normalize(first), Normalize(second));

        var exportedJson = await WaitForExportAsync(fileClient, employeeId);
        Assert.Equal(Normalize(first), Normalize(exportedJson));
    }

    private static async Task WaitForFileServiceReadyAsync(HttpClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync("/ready", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
                {
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException("File Service не стал ready за отведённое время.");
    }

    private static async Task WaitForApiAsync(HttpClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync("/", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException("Service API не стал доступен за отведённое время.");
    }

    private static async Task<string> WaitForExportAsync(HttpClient fileClient, int employeeId)
    {
        using var cts = new CancellationTokenSource(ExportTimeout);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var response = await fileClient.GetAsync($"/files/{employeeId}", cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(cts.Token);
                }

                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
        }

        throw new TimeoutException($"Файл сотрудника {employeeId} не был выгружен в объектное хранилище за отведённое время.");
    }

    private static string Normalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }
}