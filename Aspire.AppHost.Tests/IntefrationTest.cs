using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Service.Api.Entity;
using Xunit.Abstractions;
using System.Net.Http.Json;

namespace Aspire.AppHost.Tests;

public class IntegrationTest(ITestOutputHelper output) : IAsyncLifetime
{
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Aspire_AppHost>(cancellationToken);

        builder.Environment.EnvironmentName = "Development";
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
    }

    [Fact]
    public async Task TestPipeline()
    {
        var cancellationToken = CancellationToken.None;

        var random = new Random();
        var id = random.Next(1, 100);

        using var gatewayClient = _app!.CreateHttpClient("api-gw", "http");

        using var gatewayResponse = await gatewayClient.GetAsync(
            $"/api/projects?id={id}",
            cancellationToken);

        gatewayResponse.EnsureSuccessStatusCode();

        var api = await gatewayResponse.Content.ReadFromJsonAsync<ProgramProject>(
            cancellationToken: cancellationToken);

        await Task.Delay(5000, cancellationToken);

        using var storageClient = _app.CreateHttpClient("programproj-storage", "http");

        using var listResponse = await storageClient.GetAsync(
            "/api/s3",
            cancellationToken);

        listResponse.EnsureSuccessStatusCode();

        var ppList = await listResponse.Content.ReadFromJsonAsync<List<string>>(
            cancellationToken: cancellationToken);

        using var s3Response = await storageClient.GetAsync(
            $"/api/s3/programproj_{id}.json",
            cancellationToken);

        s3Response.EnsureSuccessStatusCode();

        var s3 = await s3Response.Content.ReadFromJsonAsync<ProgramProject>(
            cancellationToken: cancellationToken);

        Assert.NotNull(ppList);
        Assert.Single(ppList);

        Assert.NotNull(api);
        Assert.NotNull(s3);

        Assert.Equal(id, s3.Id);
        Assert.Equivalent(api, s3);
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