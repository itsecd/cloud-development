using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace Integration.Tests;

public sealed class BackendIntegrationTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _gatewayClient;
    private HttpClient? _fileServiceClient;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        _gatewayClient = _app.CreateHttpClient("api-gateway");
        _fileServiceClient = _app.CreateHttpClient("file-service");
    }

    public async Task DisposeAsync()
    {
        _gatewayClient?.Dispose();
        _fileServiceClient?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetPatient_ValidId_ReturnsPatient()
    {
        var response = await _gatewayClient!.GetAsync("/patient?id=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("id").GetInt32());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("fullName").GetString()));
        Assert.InRange(root.GetProperty("bloodGroup").GetInt32(), 1, 4);
    }

    [Fact]
    public async Task GetPatient_InvalidId_ReturnsBadRequest()
    {
        var response = await _gatewayClient!.GetAsync("/patient?id=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPatient_SameId_ReturnsCachedPatient()
    {
        var r1 = await _gatewayClient!.GetAsync("/patient?id=42");
        var r2 = await _gatewayClient!.GetAsync("/patient?id=42");

        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        using var doc1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
        using var doc2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync());

        Assert.Equal(
            doc1.RootElement.GetProperty("fullName").GetString(),
            doc2.RootElement.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task GetPatient_FileAppearsInMinio()
    {
        const int testId = 777;

        var genResponse = await _gatewayClient!.GetAsync($"/patient?id={testId}");
        genResponse.EnsureSuccessStatusCode();

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var filesResponse = await _fileServiceClient!.GetAsync("/files");
            if (filesResponse.IsSuccessStatusCode)
            {
                var files = JsonSerializer.Deserialize<List<string>>(
                    await filesResponse.Content.ReadAsStringAsync()) ?? [];

                if (files.Any(f => f.Contains($"patient-{testId}-")))
                    return;
            }

            await Task.Delay(1000);
        }

        Assert.Fail($"File for patient-{testId} did not appear in MinIO within 30 seconds");
    }

    [Fact]
    public async Task GetPatient_DifferentIds_ReturnDifferentPatients()
    {
        var r1 = await _gatewayClient!.GetAsync("/patient?id=10");
        var r2 = await _gatewayClient!.GetAsync("/patient?id=20");

        r1.EnsureSuccessStatusCode();
        r2.EnsureSuccessStatusCode();

        using var doc1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
        using var doc2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync());

        Assert.Equal(10, doc1.RootElement.GetProperty("id").GetInt32());
        Assert.Equal(20, doc2.RootElement.GetProperty("id").GetInt32());
    }
}
