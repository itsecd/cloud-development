using System.Net.Http.Json;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Tests;

public class BackendIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetById_ThroughGateway_ShouldPersistGeneratedPayloadToObjectStorage()
    {
        await using var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ProjectApp_AppHost>(
            ["DcpPublisher:RandomizePorts=false"]);

        await using var app = await appHost.BuildAsync();
        try
        {
            await app.StartAsync();
        }
        catch (DistributedApplicationException ex) when (ex.Message.Contains("контейнера", StringComparison.OrdinalIgnoreCase) ||
                                                         ex.Message.Contains("container runtime", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:7000") };
        var id = Random.Shared.Next(200000, 999999);

        var response = await httpClient.GetAsync($"/api/creditapplication?id={id}");
        response.EnsureSuccessStatusCode();

        var generated = await response.Content.ReadFromJsonAsync<CreditApplication>();
        Assert.NotNull(generated);
        Assert.Equal(id, generated.Id);
        Assert.False(string.IsNullOrWhiteSpace(generated.CreditType));
        Assert.True(generated.RequestedAmount > 0);
        Assert.True(generated.TermMonths > 0);
        Assert.True(generated.InterestRate > 0);
        Assert.False(string.IsNullOrWhiteSpace(generated.Status));

        using var s3 = new AmazonS3Client(
            new BasicAWSCredentials("test", "test"),
            new AmazonS3Config
            {
                ServiceURL = "http://localhost:4566",
                AuthenticationRegion = "us-east-1",
                ForcePathStyle = true
            });

        var key = await WaitForObjectKeyAsync(s3, id, TimeSpan.FromSeconds(30));
        Assert.NotNull(key);

        var obj = await s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "credit-applications",
            Key = key
        });

        using var reader = new StreamReader(obj.ResponseStream);
        var payload = await reader.ReadToEndAsync();
        var persisted = JsonSerializer.Deserialize<CreditApplication>(payload, JsonOptions);

        Assert.NotNull(persisted);
        Assert.Equivalent(generated, persisted);
    }

    private static async Task<string?> WaitForObjectKeyAsync(IAmazonS3 s3Client, int id, TimeSpan timeout)
    {
        var started = DateTime.UtcNow;
        var prefix = $"credit-applications/{id}-";

        while (DateTime.UtcNow - started < timeout)
        {
            var list = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = "credit-applications",
                Prefix = prefix
            });

            var found = list.S3Objects.FirstOrDefault()?.Key;
            if (!string.IsNullOrWhiteSpace(found))
            {
                return found;
            }

            await Task.Delay(1000);
        }

        return null;
    }
}
