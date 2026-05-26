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

        appHost.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(40);
            });
        });

        App = await appHost.BuildAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
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
        var testId = 12345;  // фиксированный ID для отладки
        var httpClient = fixture.App.CreateHttpClient("api-gateway");

        var response = await httpClient.GetAsync($"/contracts/{testId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"📦 Raw JSON: {content}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var contract = JsonSerializer.Deserialize<GenerationService.Models.SoftwareProjectContract>(content, options);

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
        var fileFound = false;

        // Увеличили время ожидания
        for (var i = 0; i < 25; i++)
        {
            await Task.Delay(4000); // 4 секунды

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

        Assert.True(fileFound, $"Файл {expectedFileName} должен появиться в S3 после обработки брокера");
    }

    [Fact]
    public async Task Contract_Can_Be_Retrieved_From_S3_By_FileName()
    {
        var testId = Random.Shared.Next(1, 100000);
        var httpClient = fixture.App.CreateHttpClient("api-gateway");

        await httpClient.GetAsync($"/contracts/{testId}");

        var fileName = $"software-project-contract-{testId}.json";
        string? fileContent = null;

        // Увеличили попытки и задержку
        for (var i = 0; i < 30; i++)
        {
            await Task.Delay(3000);
            var response = await httpClient.GetAsync($"/files/{fileName}");
            if (response.IsSuccessStatusCode)
            {
                fileContent = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(fileContent))
                    break;
            }
        }

        Assert.NotNull(fileContent);
        Assert.False(string.IsNullOrEmpty(fileContent), $"Файл {fileName} не был найден в S3");

        var savedContract = JsonSerializer.Deserialize<GenerationService.Models.SoftwareProjectContract>(fileContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(savedContract);
        Assert.Equal(testId, savedContract!.Id);
    }
}