using Aspire.Hosting;
using System.Net;
using System.Text.Json;

namespace Backend.IntegrationTests;

public sealed class EmployeeExportIntegrationTests : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(60);

    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireApp_AppHost>();
        _app = await builder.BuildAsync().WaitAsync(StartupTimeout);
        await _app.StartAsync().WaitAsync(StartupTimeout);

        var apiClient = _app.CreateHttpClient("service-api-0");
        var fileClient = _app.CreateHttpClient("file-service");

        await WaitUntilAvailableAsync(apiClient, "/");
        await WaitUntilAvailableAsync(fileClient, "/");
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

        var employeeId = 501;
        var apiClient = _app!.CreateHttpClient("service-api-0");
        var fileClient = _app.CreateHttpClient("file-service");

        using var apiResponse = await apiClient.GetAsync($"/employee?id={employeeId}");
        apiResponse.EnsureSuccessStatusCode();

        var generatedJson = await apiResponse.Content.ReadAsStringAsync();
        var exportedJson = await WaitForExportAsync(fileClient, employeeId);

        Assert.Equal(Normalize(generatedJson), Normalize(exportedJson));
    }

    [Fact]
    public async Task SameEmployeeIdReturnsCachedPayloadAndExportRemainsAvailable()
    {
        Assert.NotNull(_app);

        var employeeId = 777;
        var apiClient = _app!.CreateHttpClient("service-api-0");
        var fileClient = _app.CreateHttpClient("file-service");

        var first = await apiClient.GetStringAsync($"/employee?id={employeeId}");
        var second = await apiClient.GetStringAsync($"/employee?id={employeeId}");

        Assert.Equal(Normalize(first), Normalize(second));

        var exportedJson = await WaitForExportAsync(fileClient, employeeId);
        Assert.Equal(Normalize(first), Normalize(exportedJson));
    }

    private static async Task WaitUntilAvailableAsync(HttpClient client, string path)
    {
        using var cts = new CancellationTokenSource(StartupTimeout);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync(path, cts.Token);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
            }
            catch (TaskCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException($"Сервис не стал доступен по пути '{path}' за отведённое время.");
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

                if (response.StatusCode != HttpStatusCode.NotFound && (int)response.StatusCode >= 500)
                {
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException)
            {
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }
        }

        throw new TimeoutException($"Файл сотрудника {employeeId} не был выгружен в объектное хранилище за отведённое время.");
    }

    private static string Normalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }
}