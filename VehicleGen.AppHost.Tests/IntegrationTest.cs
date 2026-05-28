using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using VehicleGen.Api.Entities;

namespace VehicleGen.AppHost.Tests;

public class IntegrationTest : IAsyncLifetime
{
    private DistributedApplication _app = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.VehicleGen_AppHost>();
        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        builder.Configuration["LocalStack:UseLocalStack"] = "true";

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        await WaitForLocalStackReady();
    }

    private async Task WaitForLocalStackReady()
    {
        using var client = new HttpClient();
        var localstackPort = "14566";
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(60);

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var response = await client.GetAsync($"http://localhost:{localstackPort}/_localstack/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("LocalStack did not become available within 60 seconds");
    }

    [Fact]
    public async Task GatewayRequest_ShouldStoreVehicleFileInS3()
    {
        var vehicleId = 1;

        var gatewayClient = _app.CreateHttpClient("api-gateway");
        var gatewayResponse = await gatewayClient.GetAsync($"/vehicle?id={vehicleId}");
        Assert.Equal(HttpStatusCode.OK, gatewayResponse.StatusCode);

        var gatewayJson = await gatewayResponse.Content.ReadAsStringAsync();
        var gatewayVehicle = JsonSerializer.Deserialize<Vehicle>(gatewayJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(gatewayVehicle);
        Assert.Equal(vehicleId, gatewayVehicle.Id);

        await Task.Delay(TimeSpan.FromSeconds(15));

        var fileClient = _app.CreateHttpClient("file-service");
        var fileResponse = await fileClient.GetAsync($"/api/files/{vehicleId}");
        Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);

        var fileJson = await fileResponse.Content.ReadAsStringAsync();
        var fileVehicle = JsonSerializer.Deserialize<Vehicle>(fileJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(fileVehicle);
        Assert.Equal(gatewayVehicle.Id, fileVehicle.Id);
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}