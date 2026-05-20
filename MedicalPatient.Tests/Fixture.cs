using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MedicalPatient.Tests;

/// <summary>
/// Класс, поднимающий приложение для интеграционных тестов с LocalStack.
/// </summary>
public class Fixture : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _messageSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DistributedApplication App { get; private set; } = null!;
    public AmazonS3Client S3Client { get; private set; } = null!;
    public AmazonSQSClient SQSClient { get; private set; } = null!;
    public string SqsUrl { get; private set; } = string.Empty;
    public string S3Url { get; private set; } = string.Empty;
    public const string BucketName = "medical-patient";
    public const string QueueName = "medical-patients";

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MedicalPatient_AppHost_AppHost>(
            [
                "DcpPublisher:RandomizePorts=false"
            ]);

        appHost.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(1);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
            }));

        App = await appHost.BuildAsync();
        await App.StartAsync(cts.Token);

        await Task.WhenAll(
            App.ResourceNotifications.WaitForResourceAsync("localstack"),
            App.ResourceNotifications.WaitForResourceAsync("medicalpatient-apigateway"),
            App.ResourceNotifications.WaitForResourceAsync("medicalpatient-fileservice"),
            App.ResourceNotifications.WaitForResourceAsync("generator-1"),
            App.ResourceNotifications.WaitForResourceAsync("generator-2"),
            App.ResourceNotifications.WaitForResourceAsync("generator-3")
        ).WaitAsync(TimeSpan.FromMinutes(5));

        await Task.Delay(TimeSpan.FromSeconds(5));

        var localstackClient = App.CreateHttpClient("localstack", "http");
        var localstackUrl = localstackClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:4566";
        S3Url = localstackUrl;
        SqsUrl = localstackUrl;

        S3Client = new AmazonS3Client(
            new BasicAWSCredentials("test", "test"),
            new AmazonS3Config
            {
                ServiceURL = S3Url,
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            });

        SQSClient = new AmazonSQSClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonSQSConfig
            {
                ServiceURL = SqsUrl,
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            });

        await EnsureBucketExistsAsync();
        await EnsureQueueExistsAsync();
    }

    private async Task EnsureBucketExistsAsync()
    {
        var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(S3Client, BucketName);

        if (!bucketExists)
        {
            await S3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = BucketName,
                UseClientRegion = true
            });
        }
    }

    private async Task EnsureQueueExistsAsync()
    {
        try
        {
            await SQSClient.GetQueueUrlAsync(QueueName);
        }
        catch (AmazonSQSException ex) when (ex.ErrorCode == "AWS.SimpleQueueService.NonExistentQueue")
        {
            await SQSClient.CreateQueueAsync(new CreateQueueRequest
            {
                QueueName = QueueName,
                Attributes = new Dictionary<string, string>
                {
                    { "VisibilityTimeout", "30" }
                }
            });
        }
    }

    public async Task<string> GetQueueUrlAsync()
    {
        var response = await SQSClient.GetQueueUrlAsync(QueueName);
        return response.QueueUrl;
    }

    public async Task SendMessageToQueueAsync<T>(T message)
    {
        var queueUrl = await GetQueueUrlAsync();
        var messageBody = JsonSerializer.Serialize(message, _messageSerializerOptions);

        await SQSClient.SendMessageAsync(queueUrl, messageBody);
    }

    public async Task<List<S3Object>> WaitForS3ObjectAsync(string keyPrefix, int maxAttempts = 15)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            var response = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = keyPrefix,
            });

            if (response.S3Objects.Count > 0)
                return response.S3Objects;
        }

        return [];
    }

    public async Task DisposeAsync()
    {
        S3Client?.Dispose();
        SQSClient?.Dispose();

        if (App is not null)
        {
            await App.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
            await App.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}
