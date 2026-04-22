using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Inventory.ApiService.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit.Abstractions;

namespace Inventory.Tests;

public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private IDistributedApplicationTestingBuilder? _builder;
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;

        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Inventory_AppHost>(cancellationToken);
        _builder.Configuration["DcpPublisher:RandomizePorts"] = "false";

        _builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });
    }

    [Fact]
    public async Task TestPipeline()
    {
        var cancellationToken = CancellationToken.None;

        _app = await _builder!.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);

        var random = new Random();
        var id = random.Next(1, 100);

        using var gatewayClient = _app.CreateHttpClient("apigateway", "https");
        using var gatewayResponse = await gatewayClient.GetAsync($"/api/inventory?id={id}", cancellationToken);

        var apiProduct = JsonSerializer.Deserialize<Product>(
            await gatewayResponse.Content.ReadAsStringAsync(cancellationToken));

        await Task.Delay(5000, cancellationToken);

        using var sinkClient = _app.CreateHttpClient("inventory-files", "http");

        using var listResponse = await sinkClient.GetAsync("/api/s3", cancellationToken);
        var inventoryList = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync(cancellationToken));

        using var s3Response = await sinkClient.GetAsync($"/api/s3/inventory_{id}.json", cancellationToken);
        var s3Product = JsonSerializer.Deserialize<Product>(
            await s3Response.Content.ReadAsStringAsync(cancellationToken));

        Assert.NotNull(inventoryList);
        Assert.Single(inventoryList);
        Assert.NotNull(apiProduct);
        Assert.NotNull(s3Product);
        Assert.Equal(id, s3Product!.Id);
        Assert.Equivalent(apiProduct, s3Product);
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        if (_builder != null)
            await _builder.DisposeAsync();
    }
}