using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace IntegrationTests;

public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CloudDevelopment_AppHost>();

        // Упрощённая конфигурация с большими таймаутами
        appHost.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            });
        });

        App = await appHost.BuildAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        await App.StartAsync(cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (App != null)
            await App.DisposeAsync();
    }
}

public class IntegrationTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    [Fact]
    public async Task Contract_Generated_Through_Gateway_Returns_Correct_Data()
    {
        var testId = Random.Shared.Next(1, 100000);
        var httpClient = fixture.App.CreateHttpClient("api-gateway");

        var response = await httpClient.GetAsync($"/contracts/{testId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var contract = JsonSerializer.Deserialize<GenerationService.Models.SoftwareProjectContract>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(contract);
        Assert.Equal(testId, contract!.Id);
    }

    [Fact]
    public async Task Repeated_Requests_Return_Cached_Response()
    {
        var testId = Random.Shared.Next(1, 100000);
        var httpClient = fixture.App.CreateHttpClient("api-gateway");

        var response1 = await httpClient.GetAsync($"/contracts/{testId}");
        var content1 = await response1.Content.ReadAsStringAsync();

        var response2 = await httpClient.GetAsync($"/contracts/{testId}");
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(content1, content2);
    }

    [Fact]
    public async Task Contract_Is_Saved_To_S3_And_Available_In_Files_List()
    {
        var testId = Random.Shared.Next(1, 100000);
        var httpClient = fixture.App.CreateHttpClient("api-gateway");

        await httpClient.GetAsync($"/contracts/{testId}");

        var expectedFileName = $"software-project-contract-{testId}.json";
        bool fileFound = false;

        for (int i = 0; i < 30; i++) // 2 минуты ожидания
        {
            await Task.Delay(4000);

            var filesResponse = await httpClient.GetAsync("/files");
            if (filesResponse.IsSuccessStatusCode)
            {
                var filesContent = await filesResponse.Content.ReadAsStringAsync();
                var files = JsonSerializer.Deserialize<List<string>>(filesContent);

                if (files?.Contains(expectedFileName) == true)
                {
                    fileFound = true;
                    break;
                }
            }
        }

        Assert.True(fileFound, $"Файл {expectedFileName} должен появиться в S3");
    }

    [Fact]
    public async Task Contract_Can_Be_Retrieved_From_S3_By_FileName()
    {
        var testId = Random.Shared.Next(1, 100000);
        var httpClient = fixture.App.CreateHttpClient("api-gateway");

        await httpClient.GetAsync($"/contracts/{testId}");

        var fileName = $"software-project-contract-{testId}.json";
        string? fileContent = null;

        for (int i = 0; i < 35; i++)
        {
            await Task.Delay(3500);
            var response = await httpClient.GetAsync($"/files/{fileName}");
            if (response.IsSuccessStatusCode)
            {
                fileContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(fileContent))
                    break;
            }
        }

        Assert.NotNull(fileContent);
        Assert.False(string.IsNullOrEmpty(fileContent), $"Файл {fileName} не найден в S3");

        var savedContract = JsonSerializer.Deserialize<GenerationService.Models.SoftwareProjectContract>(fileContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(savedContract);
        Assert.Equal(testId, savedContract!.Id);
    }
}