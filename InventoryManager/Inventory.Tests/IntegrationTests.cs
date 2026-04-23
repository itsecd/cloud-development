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
        Assert.NotNull(_builder);

        _app = await _builder.BuildAsync();
        await _app.StartAsync();

        await Task.Delay(10000);

        var id = Random.Shared.Next(1, 100);

        using var gatewayClient = _app.CreateHttpClient("apigateway", "https");
        using var gatewayResponse = await gatewayClient.GetAsync($"/api/inventory?id={id}");

        var gatewayContent = await gatewayResponse.Content.ReadAsStringAsync();
        Assert.True(
            gatewayResponse.IsSuccessStatusCode,
            $"Gateway failed: {gatewayResponse.StatusCode} - {gatewayContent}");

        var apiProduct = JsonSerializer.Deserialize<Product>(gatewayContent);
        Assert.NotNull(apiProduct);
        Assert.Equal(id, apiProduct.Id);

        await Task.Delay(5000);

        using var storageClient = _app.CreateHttpClient("inventory-files", "http");

        using var listResponse = await storageClient.GetAsync("/api/s3");
        var listContent = await listResponse.Content.ReadAsStringAsync();

        Assert.True(
            listResponse.IsSuccessStatusCode,
            $"Storage list failed: {listResponse.StatusCode} - {listContent}");

        var inventoryList = JsonSerializer.Deserialize<List<string>>(listContent);
        Assert.NotNull(inventoryList);
        Assert.NotEmpty(inventoryList);

        var matchingFile = inventoryList.FirstOrDefault(f => f.Contains($"inventory_{id}"));
        Assert.NotNull(matchingFile);

        using var s3Response = await storageClient.GetAsync($"/api/s3/{matchingFile}");
        var s3Content = await s3Response.Content.ReadAsStringAsync();

        Assert.True(
            s3Response.IsSuccessStatusCode,
            $"Storage read failed: {s3Response.StatusCode} - {s3Content}");

        var s3Product = JsonSerializer.Deserialize<Product>(s3Content);
        Assert.NotNull(s3Product);
        Assert.Equal(id, s3Product.Id);
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
        {
            await _builder.DisposeAsync();
        }
    }
}