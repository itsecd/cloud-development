using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;

namespace Integration.Tests;

public sealed class AppHostFixture : IAsyncLifetime
{
    public HttpClient GatewayClient { get; private set; } = null!;
    public HttpClient FileServiceClient { get; private set; } = null!;

    private DistributedApplication _app = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        GatewayClient = _app.CreateHttpClient("api-gateway");
        FileServiceClient = _app.CreateHttpClient("file-service");

        // Poll until the gateway is actually responding (containers + services need time to warm up)
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        while (true)
        {
            try
            {
                var response = await GatewayClient.GetAsync("/patient?id=1", cts.Token);
                if ((int)response.StatusCode < 500)
                    break;
            }
            catch
            {
                // not ready yet
            }
            await Task.Delay(2000, cts.Token);
        }
    }

    public async Task DisposeAsync()
    {
        GatewayClient?.Dispose();
        FileServiceClient?.Dispose();
        await _app.DisposeAsync();
    }
}
