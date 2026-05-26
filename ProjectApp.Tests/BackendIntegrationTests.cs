using System.Net.Http.Json;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting.Testing;
using ProjectApp.Domain.Entities;
using ProjectApp.FileService.Storage;

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
        await app.StartAsync();

        using var httpClient = app.CreateHttpClient("projectapp-gateway");
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

        var key = ICreditApplicationObjectStorage.BuildObjectKey(id);
        await WaitForObjectAsync(s3, key, TimeSpan.FromSeconds(30));

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

    private static async Task WaitForObjectAsync(IAmazonS3 s3Client, string key, TimeSpan timeout)
    {
        var started = DateTime.UtcNow;

        while (DateTime.UtcNow - started < timeout)
        {
            try
            {
                await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = "credit-applications",
                    Key = key
                });
                return;
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode is "NoSuchBucket" or "NoSuchKey" or "NotFound")
            {
            }

            await Task.Delay(1000);
        }

        Assert.Fail($"Object {key} was not persisted to object storage.");
    }
}
