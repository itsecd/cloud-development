using Amazon.CDK;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace CompanyEmployee.Test;

public class Fixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;
    public AmazonS3Client S3Client { get; private set; } = null!;
    public HttpClient GatewayClient { get; private set; } = null!;

    private const string BucketName = "companyemployee";

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CompanyEmployee_AppHost>(
            [
                "DcpPublisher:RandomizePorts=false"
            ]);

        App = await appHost.BuildAsync(cts.Token);
        await App.StartAsync(cts.Token);

        await Task.WhenAll(
            App.ResourceNotifications.WaitForResourceAsync("minio"),
            App.ResourceNotifications.WaitForResourceAsync("companyemployee-localstack"),
            App.ResourceNotifications.WaitForResourceAsync("companyemployee-apigateway"),
            App.ResourceNotifications.WaitForResourceAsync("fileservice")
        ).WaitAsync(TimeSpan.FromMinutes(5));

        GatewayClient = App.CreateHttpClient("companyemployee-apigateway");

        var minioEndpoint = App.GetEndpoint("minio", "http");
        var minioUrl = $"http://{minioEndpoint.Host}:{minioEndpoint.Port}";

        S3Client = new AmazonS3Client(
            new BasicAWSCredentials("minioadmin", "minioadmin"),
            new AmazonS3Config
            {
                ServiceURL = minioUrl,
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            });

        await Task.Delay(TimeSpan.FromSeconds(3));

        var doesExist = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(S3Client, BucketName);
        if (!doesExist)
        {
            await S3Client.PutBucketAsync(new PutBucketRequest { BucketName = BucketName });
        }
    }

    public async Task<List<S3Object>> WaitForS3ObjectAsync(string key)
    {

        await Task.Delay(TimeSpan.FromSeconds(2));

        var response = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = BucketName,
            Prefix = key,
        });

        if (response.S3Objects is not null && response.S3Objects.Count > 0)
            return response.S3Objects;

        return [];
    }

    public async Task DisposeAsync(){
        S3Client?.Dispose();
        GatewayClient?.Dispose();
        await App.StopAsync();
        await App.DisposeAsync().AsTask();
    }
}