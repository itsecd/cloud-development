using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CreditApp.IntegrationTests;

/// <summary>
/// Фикстура для поднятия Aspire AppHost один раз на все тесты.
/// </summary>
public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;
    public AmazonS3Client S3Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CreditApp_AppHost>();

        appHost.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
                options.Retry.MaxRetryAttempts = 10;
                options.Retry.Delay = TimeSpan.FromSeconds(3);
            }));

        App = await appHost.BuildAsync();
        await App.StartAsync();

        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("creditapp-gateway")
            .WaitAsync(TimeSpan.FromMinutes(3));

        var localStackUrl = App.GetEndpoint("localstack", "localstack").ToString().TrimEnd('/');
        S3Client = new AmazonS3Client("test", "test", new AmazonS3Config
        {
            ServiceURL = localStackUrl,
            ForcePathStyle = true
        });
    }

    /// <summary>
    /// Ожидает появления файла в S3 с указанным префиксом.
    /// </summary>
    public async Task<List<S3Object>> WaitForS3ObjectAsync(string prefix, int maxAttempts = 15)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            var listResponse = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = "credit-files",
                Prefix = prefix
            });

            if (listResponse.S3Objects.Count > 0)
                return listResponse.S3Objects;
        }

        return [];
    }

    public async Task DisposeAsync()
    {
        S3Client.Dispose();

        try
        {
            await App.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException) { }
        catch (OperationCanceledException) { }
    }
}
